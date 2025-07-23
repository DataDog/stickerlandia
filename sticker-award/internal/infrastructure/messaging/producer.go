package messaging

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/datadoghq/stickerlandia/sticker-award/internal/config"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/events"
	"github.com/segmentio/kafka-go"
	"go.uber.org/zap"
)

// Producer handles publishing events to Kafka
type Producer struct {
	writer *kafka.Writer
	logger *zap.SugaredLogger
}

// Topics for different event types
const (
	TopicStickerAssignedToUser  = "stickers.stickerAssignedToUser.v1"
	TopicStickerRemovedFromUser = "stickers.stickerRemovedFromUser.v1"
	TopicStickerClaimed         = "users.stickerClaimed.v1"
)

// NewProducer creates a new Kafka producer
func NewProducer(cfg *config.KafkaConfig, logger *zap.SugaredLogger) (*Producer, error) {
	writer := &kafka.Writer{
		Addr:         kafka.TCP(cfg.Brokers...),
		Balancer:     &kafka.LeastBytes{},
		WriteTimeout: time.Duration(cfg.ProducerTimeout) * time.Millisecond,
		ReadTimeout:  time.Duration(cfg.ProducerTimeout) * time.Millisecond,
		RequiredAcks: kafka.RequiredAcks(cfg.RequireAcks),
		Async:        false, // Synchronous writes
		Compression:  kafka.Snappy,
		BatchSize:    cfg.ProducerBatchSize,
		BatchTimeout: 100 * time.Millisecond,
		MaxAttempts:  cfg.ProducerRetries,
	}

	return &Producer{
		writer: writer,
		logger: logger,
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

// publishEvent publishes an event to the specified topic
func (p *Producer) publishEvent(topic, key string, event interface{}) error {
	// Serialize event to JSON
	eventBytes, err := json.Marshal(event)
	if err != nil {
		p.logger.Errorw("Failed to serialize event", "error", err, "topic", topic)
		return fmt.Errorf("failed to serialize event: %w", err)
	}

	// Create Kafka message
	msg := kafka.Message{
		Topic: topic,
		Key:   []byte(key),
		Value: eventBytes,
		Headers: []kafka.Header{
			{
				Key:   "content-type",
				Value: []byte("application/json"),
			},
		},
		Time: time.Now(),
	}

	// Send message
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	err = p.writer.WriteMessages(ctx, msg)
	if err != nil {
		p.logger.Errorw("Failed to publish event",
			"error", err,
			"topic", topic,
			"key", key)
		return fmt.Errorf("failed to publish event to topic %s: %w", topic, err)
	}

	p.logger.Infow("Successfully published event",
		"topic", topic,
		"key", key)

	return nil
}

// Close closes the producer
func (p *Producer) Close() error {
	if p.writer != nil {
		return p.writer.Close()
	}
	return nil
}
