// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package messaging

import (
	"context"
	"fmt"
	log "github.com/sirupsen/logrus"
	"os"
	"os/signal"
	"sync"
	"syscall"

	"github.com/IBM/sarama"
)

// Consumer represents a Kafka consumer with Datadog DSM integration
type Consumer struct {
	client   sarama.ConsumerGroup
	handlers map[string]MessageHandler
	groupID  string
	brokers  []string
	topics   []string
	ready    chan bool
	ctx      context.Context
	cancel   context.CancelFunc
	wg       sync.WaitGroup
}

// MessageHandler defines the interface for handling different message types
type MessageHandler interface {
	Handle(ctx context.Context, message *sarama.ConsumerMessage) error
	Topic() string
}

// NewConsumer creates a new Kafka consumer with Datadog DSM integration
func NewConsumer(brokers []string, groupID string) (*Consumer, error) {
	// Configure Sarama to use logrus for consistent JSON logging
	sarama.Logger = log.StandardLogger()

	log.WithFields(log.Fields{
		"brokers":          brokers,
		"groupID":          groupID,
		"protocol_version": "2.1.0.0",
	}).Info("Creating Kafka consumer")

	// Create shared Sarama configuration
	config := NewSaramaConfig("sticker-award-consumer")
	ConfigureConsumer(config)

	log.WithFields(log.Fields{
		"brokers": brokers,
		"groupID": groupID,
	}).Info("Attempting to create Sarama consumer group")

	client, err := sarama.NewConsumerGroup(brokers, groupID, config)
	if err != nil {
		log.WithFields(log.Fields{
			"brokers": brokers,
			"groupID": groupID,
			"error":   err,
		}).Error("Failed to create Sarama consumer group")
		return nil, fmt.Errorf("failed to create consumer group: %w", err)
	}

	ctx, cancel := context.WithCancel(context.Background())

	consumer := Consumer{
		client:   client,
		handlers: make(map[string]MessageHandler),
		groupID:  groupID,
		brokers:  brokers,
		ready:    make(chan bool),
		ctx:      ctx,
		cancel:   cancel,
	}

	return &consumer, nil
}

// RegisterHandler registers a message handler for a specific topic
func (c *Consumer) RegisterHandler(handler MessageHandler) {
	c.handlers[handler.Topic()] = handler
	c.topics = append(c.topics, handler.Topic())
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
