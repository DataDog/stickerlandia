// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package kafka

import (
	"context"
	"fmt"
	"os"
	"os/signal"
	"sync"
	"syscall"

	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events/consumed"
	log "github.com/sirupsen/logrus"

	"github.com/IBM/sarama"
)

// CloudEventMessageHandler is a type alias for convenience
type CloudEventMessageHandler[T any] messaging.CloudEventMessageHandler[T]

// Consumer represents a Kafka consumer with Datadog DSM integration
type Consumer struct {
	client      sarama.ConsumerGroup
	handlers    map[string]MessageHandler
	serviceName string
	groupID     string
	brokers     []string
	topics      []string
	ready       chan bool
	ctx         context.Context
	cancel      context.CancelFunc
	wg          sync.WaitGroup
}

// MessageHandler defines the interface for handling different message types
type MessageHandler interface {
	Handle(ctx context.Context, message *sarama.ConsumerMessage) error
	Topic() string
}

// NewConsumer creates a new Kafka consumer with Datadog DSM integration
func NewConsumer(cfg *config.KafkaConfig, serviceName string) (*Consumer, error) {
	// Configure Sarama to use logrus for consistent JSON logging
	sarama.Logger = log.StandardLogger()

	log.WithFields(log.Fields{
		"brokers":          cfg.Brokers,
		"groupID":          cfg.GroupID,
		"protocol_version": "2.1.0.0",
	}).Info("Creating Kafka consumer")

	// Create shared Sarama configuration
	config := NewSaramaConfig("sticker-award-consumer", cfg)
	ConfigureConsumer(config)

	log.WithFields(log.Fields{
		"brokers": cfg.Brokers,
		"groupID": cfg.GroupID,
	}).Info("Attempting to create Sarama consumer group")

	client, err := sarama.NewConsumerGroup(cfg.Brokers, cfg.GroupID, config)
	if err != nil {
		log.WithFields(log.Fields{
			"brokers": cfg.Brokers,
			"groupID": cfg.GroupID,
			"error":   err,
		}).Error("Failed to create Sarama consumer group")
		return nil, fmt.Errorf("failed to create consumer group: %w", err)
	}

	ctx, cancel := context.WithCancel(context.Background())

	consumer := Consumer{
		client:      client,
		handlers:    make(map[string]MessageHandler),
		serviceName: serviceName,
		groupID:     cfg.GroupID,
		brokers:     cfg.Brokers,
		ready:       make(chan bool),
		ctx:         ctx,
		cancel:      cancel,
	}

	return &consumer, nil
}

// RegisterHandler registers a message handler for a specific topic.
// The handler can be either:
// 1. A CloudEventMessageHandler[T] that will be wrapped with Kafka middleware
// 2. An already-wrapped kafka.MessageHandler
func (c *Consumer) RegisterHandler(handler interface{}) {
	// First check if it's already a wrapped MessageHandler
	if h, ok := handler.(MessageHandler); ok {
		c.handlers[h.Topic()] = h
		c.topics = append(c.topics, h.Topic())
		return
	}

	// Otherwise, try to wrap as CloudEventMessageHandler
	// Get operation name if available
	type operationNamer interface {
		OperationName() string
		Topic() string
	}

	var operationName, topic string
	if on, ok := handler.(operationNamer); ok {
		operationName = on.OperationName()
		topic = on.Topic()
	} else {
		log.WithFields(log.Fields{
			"handlerType": fmt.Sprintf("%T", handler),
		}).Panic("RegisterHandler requires handler with OperationName() and Topic() methods")
	}

	// Wrap the CloudEventMessageHandler with Kafka middleware
	var wrapped MessageHandler
	wrapped = c.wrapCloudEventHandler(handler, operationName)

	if wrapped == nil {
		log.WithFields(log.Fields{
			"handlerType": fmt.Sprintf("%T", handler),
		}).Panic("RegisterHandler received unsupported CloudEventMessageHandler type")
	}

	c.handlers[topic] = wrapped
	c.topics = append(c.topics, topic)
}

// wrapCloudEventHandler wraps a CloudEventMessageHandler with Kafka middleware.
// This uses a type switch to handle known event types.
func (c *Consumer) wrapCloudEventHandler(handler interface{}, operationName string) MessageHandler {
	// Import the consumed events package to access event types
	// Type switch on known CloudEventMessageHandler types
	switch h := handler.(type) {
	case CloudEventMessageHandler[consumed.UserRegisteredEvent]:
		return NewMessagingHandler(h, operationName, c.serviceName)
	default:
		return nil
	}
}

// Start starts the consumer
func (c *Consumer) Start() error {
	if len(c.topics) == 0 {
		return fmt.Errorf("no topics registered")
	}

	log.WithFields(log.Fields{
		"group_id": c.groupID,
		"topics":   c.topics,
		"brokers":  c.brokers,
	}).Info("Starting Kafka consumer")

	c.wg.Add(1)
	go func() {
		defer c.wg.Done()
		for {
			select {
			case <-c.ctx.Done():
				log.WithContext(c.ctx).Info("Kafka consumer context cancelled")
				return
			default:
				if err := c.client.Consume(c.ctx, c.topics, c); err != nil {
					log.WithFields(log.Fields{"error": err}).Error("Error from consumer")
					return
				}
			}
		}
	}()

	// Handle consumer errors
	c.wg.Add(1)
	go func() {
		defer c.wg.Done()
		for {
			select {
			case err := <-c.client.Errors():
				if err != nil {
					log.WithContext(c.ctx).WithFields(log.Fields{"error": err}).Error("Consumer error")
				}
			case <-c.ctx.Done():
				return
			}
		}
	}()

	<-c.ready
	log.WithContext(c.ctx).Info("Kafka consumer ready")

	// Handle shutdown signals
	sigterm := make(chan os.Signal, 1)
	signal.Notify(sigterm, syscall.SIGINT, syscall.SIGTERM)

	select {
	case <-c.ctx.Done():
		log.WithContext(c.ctx).Info("Context cancelled")
	case <-sigterm:
		log.WithContext(c.ctx).Info("Shutdown signal received")
	}

	return nil
}

// Stop stops the consumer
func (c *Consumer) Stop() error {
	log.WithContext(c.ctx).Info("Stopping Kafka consumer...")
	c.cancel()

	if err := c.client.Close(); err != nil {
		log.WithFields(log.Fields{"error": err}).Error("Error closing consumer client")
		return err
	}

	c.wg.Wait()
	log.WithContext(c.ctx).Info("Kafka consumer stopped")
	return nil
}

// Setup is run at the beginning of a new session, before ConsumeClaim
func (c *Consumer) Setup(sarama.ConsumerGroupSession) error {
	close(c.ready)
	return nil
}

// Cleanup is run at the end of a session, once all ConsumeClaim goroutines have exited
func (c *Consumer) Cleanup(sarama.ConsumerGroupSession) error {
	return nil
}

// ConsumeClaim must start a consumer loop of ConsumerGroupClaim's Messages().
func (c *Consumer) ConsumeClaim(session sarama.ConsumerGroupSession, claim sarama.ConsumerGroupClaim) error {
	for {
		select {
		case message := <-claim.Messages():
			if message == nil {
				return nil
			}

			// Get handler for this topic
			handler, exists := c.handlers[message.Topic]
			if !exists {
				log.WithContext(session.Context()).WithFields(log.Fields{"topic": message.Topic}).Warn("No handler registered for topic")
				session.MarkMessage(message, "")
				continue
			}

			// Process message - middleware handles DSM, tracing, and CloudEvent parsing
			err := handler.Handle(session.Context(), message)
			if err != nil {
				log.WithContext(session.Context()).WithFields(log.Fields{
					"topic":     message.Topic,
					"partition": message.Partition,
					"offset":    message.Offset,
					"error":     err,
				}).Error("Error handling message")
			} else {
				log.WithContext(session.Context()).WithFields(log.Fields{
					"topic":     message.Topic,
					"partition": message.Partition,
					"offset":    message.Offset,
				}).Debug("Message processed successfully")
			}

			session.MarkMessage(message, "")

		case <-session.Context().Done():
			return nil
		}
	}
}

// DatadogInterceptor implements Datadog DSM for Kafka consumers
type DatadogInterceptor struct{}

// NewDatadogInterceptor creates a new Datadog interceptor
func NewDatadogInterceptor() *DatadogInterceptor {
	return &DatadogInterceptor{}
}

// OnConsume is called when a message is consumed
func (d *DatadogInterceptor) OnConsume(message *sarama.ConsumerMessage) {
	// Datadog DSM tracking will be handled in the main consume loop
	// This interceptor is mainly for setup and can be extended if needed
}
