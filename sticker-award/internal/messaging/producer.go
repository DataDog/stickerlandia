package messaging

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	ddsamarama "github.com/DataDog/dd-trace-go/contrib/IBM/sarama/v2"
	"github.com/DataDog/dd-trace-go/v2/datastreams"
	"github.com/DataDog/dd-trace-go/v2/datastreams/options"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/IBM/sarama"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events/published"
	"go.uber.org/zap"
)

// Producer handles publishing events to Kafka using Sarama
type Producer struct {
	producer sarama.SyncProducer
	logger   *zap.SugaredLogger
}

// Topics for different event types
const (
	TopicStickerAssignedToUser  = "stickers.stickerAssignedToUser.v1"
	TopicStickerRemovedFromUser = "stickers.stickerRemovedFromUser.v1"
	TopicStickerClaimed         = "users.stickerClaimed.v1"
)

// NewProducer creates a new Kafka producer using Sarama with Datadog instrumentation
func NewProducer(cfg *config.KafkaConfig, logger *zap.SugaredLogger) (*Producer, error) {
	// Create shared Sarama configuration
	config := NewSaramaConfig("sticker-award-producer")
	ConfigureProducer(config, cfg)

	// Create base Sarama producer first
	baseProducer, err := sarama.NewSyncProducer(cfg.Brokers, config)
	if err != nil {
		return nil, fmt.Errorf("failed to create base Sarama producer: %w", err)
	}

	// Wrap with Datadog instrumentation
	producer := ddsamarama.WrapSyncProducer(config, baseProducer,
		ddsamarama.WithService("sticker-award"),
		ddsamarama.WithAnalytics(true),
		ddsamarama.WithDataStreams(),
	)

	return &Producer{
		producer: producer,
		logger:   logger,
	}, nil
}

// PublishStickerAssignedEvent publishes a sticker assigned event
func (p *Producer) PublishStickerAssignedEvent(ctx context.Context, event *published.StickerAssignedToUserEvent) error {
	return p.publishEvent(ctx, TopicStickerAssignedToUser, event.AccountID, event)
}

// PublishStickerRemovedEvent publishes a sticker removed event
func (p *Producer) PublishStickerRemovedEvent(ctx context.Context, event *published.StickerRemovedFromUserEvent) error {
	return p.publishEvent(ctx, TopicStickerRemovedFromUser, event.AccountID, event)
}

// PublishStickerClaimedEvent publishes a sticker claimed event
func (p *Producer) PublishStickerClaimedEvent(ctx context.Context, event *published.StickerClaimedEvent) error {
	return p.publishEvent(ctx, TopicStickerClaimed, event.AccountID, event)
}

// publishEvent publishes an event to the specified topic using Sarama
func (p *Producer) publishEvent(ctx context.Context, topic, key string, event interface{}) error {
	// Wrap event in CloudEvent format with W3C trace context for span links
	cloudEvent := events.NewCloudEvent(ctx, topic, "sticker-award", event)

	// Serialize CloudEvent to JSON
	eventBytes, err := json.Marshal(cloudEvent)
	if err != nil {
		p.logger.Errorw("Failed to serialize cloud event", "error", err, "topic", topic)
		return fmt.Errorf("failed to serialize cloud event: %w", err)
	}

	// Create Sarama producer message
	msg := &sarama.ProducerMessage{
		Topic: topic,
		Key:   sarama.StringEncoder(key),
		Value: sarama.ByteEncoder(eventBytes),
		Headers: []sarama.RecordHeader{
			{
				Key:   []byte("content-type"),
				Value: []byte("application/json"),
			},
		},
		Timestamp: time.Now(),
	}

	// Set manual DSM checkpoint for outbound message with service override
	ctx, ok := tracer.SetDataStreamsCheckpointWithParams(ctx, options.CheckpointParams{
		ServiceOverride: "sticker-award",
	}, "direction:out", "type:kafka", "topic:"+topic, "manual_checkpoint:true")

	// Inject DSM context into message headers if checkpoint was successful
	if ok {
		datastreams.InjectToBase64Carrier(ctx, ddsamarama.NewProducerMessageCarrier(msg))
	}

	// Inject APM trace context into message headers from current context
	if span, ok := tracer.SpanFromContext(ctx); ok {
		_ = tracer.Inject(span.Context(), ddsamarama.NewProducerMessageCarrier(msg))
	}

	// Send message synchronously - the Datadog wrapper will create the APM span automatically
	partition, offset, err := p.producer.SendMessage(msg)
	if err != nil {
		p.logger.Errorw("Failed to publish event",
			"error", err,
			"topic", topic,
			"key", key)
		return fmt.Errorf("failed to publish event to topic %s: %w", topic, err)
	}

	p.logger.Infow("Successfully published event",
		"topic", topic,
		"key", key,
		"partition", partition,
		"offset", offset)

	return nil
}

// Close closes the producer
func (p *Producer) Close() error {
	if p.producer != nil {
		return p.producer.Close()
	}
	return nil
}
