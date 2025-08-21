package messaging

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/IBM/sarama"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/domain/events"
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

// NewProducer creates a new Kafka producer using Sarama
func NewProducer(cfg *config.KafkaConfig, logger *zap.SugaredLogger) (*Producer, error) {
	config := sarama.NewConfig()

	// Producer configuration matching the original kafka-go settings
	config.Producer.Return.Successes = true
	config.Producer.Return.Errors = true
	config.Producer.RequiredAcks = sarama.RequiredAcks(cfg.RequireAcks)
	config.Producer.Retry.Max = cfg.ProducerRetries
	config.Producer.Timeout = time.Duration(cfg.ProducerTimeout) * time.Millisecond
	config.Producer.Compression = sarama.CompressionSnappy
	config.Producer.Flush.Bytes = cfg.ProducerBatchSize
	config.Producer.Flush.Frequency = 100 * time.Millisecond
	config.Version = sarama.V2_1_0_0
	config.ClientID = "sticker-award-producer"

	// Network timeouts
	config.Net.DialTimeout = 10 * time.Second
	config.Net.ReadTimeout = 10 * time.Second
	config.Net.WriteTimeout = 10 * time.Second

	producer, err := sarama.NewSyncProducer(cfg.Brokers, config)
	if err != nil {
		return nil, fmt.Errorf("failed to create Sarama producer: %w", err)
	}

	return &Producer{
		producer: producer,
		logger:   logger,
	}, nil
}

// PublishStickerAssignedEvent publishes a sticker assigned event
func (p *Producer) PublishStickerAssignedEvent(event *events.StickerAssignedToUserEvent) error {
	return p.publishEvent(TopicStickerAssignedToUser, event.AccountID, event)
}

// PublishStickerRemovedEvent publishes a sticker removed event
func (p *Producer) PublishStickerRemovedEvent(event *events.StickerRemovedFromUserEvent) error {
	return p.publishEvent(TopicStickerRemovedFromUser, event.AccountID, event)
}

// PublishStickerClaimedEvent publishes a sticker claimed event
func (p *Producer) PublishStickerClaimedEvent(event *events.StickerClaimedEvent) error {
	return p.publishEvent(TopicStickerClaimed, event.AccountID, event)
}

// publishEvent publishes an event to the specified topic using Sarama
func (p *Producer) publishEvent(topic, key string, event interface{}) error {
	// Serialize event to JSON
	eventBytes, err := json.Marshal(event)
	if err != nil {
		p.logger.Errorw("Failed to serialize event", "error", err, "topic", topic)
		return fmt.Errorf("failed to serialize event: %w", err)
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

	// Send message synchronously
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
