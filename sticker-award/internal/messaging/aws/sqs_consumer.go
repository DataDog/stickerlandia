// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package aws

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"os/signal"
	"sync"
	"syscall"
	"time"

	"github.com/aws/aws-sdk-go-v2/aws"
	awsconfig "github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/service/sqs"
	"github.com/aws/aws-sdk-go-v2/service/sqs/types"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events/consumed"
	log "github.com/sirupsen/logrus"
)

// CloudEventMessageHandler is a type alias for convenience
type CloudEventMessageHandler[T any] messaging.CloudEventMessageHandler[T]

// Consumer represents an SQS consumer with Datadog DSM integration
type Consumer struct {
	client            *sqs.Client
	queueURL          string
	serviceName       string
	handlers          map[string]MessageHandler
	maxConcurrency    int
	visibilityTimeout int32
	waitTimeSeconds   int32
	ctx               context.Context
	cancel            context.CancelFunc
	wg                sync.WaitGroup
}

// MessageHandler defines the interface for handling different message types
type MessageHandler interface {
	Handle(ctx context.Context, message *SQSMessage) error
	Topic() string
}

// SQSMessage represents a message received from SQS with EventBridge envelope
type SQSMessage struct {
	Body       string
	Attributes map[string]string
	Topic      string
	RawMessage types.Message
}

// NewSQSConsumer creates a new SQS consumer with Datadog DSM integration
func NewSQSConsumer(cfg *config.AWSConfig, serviceName string) (*Consumer, error) {
	// Validate required configuration
	if cfg.UserRegisteredQueueURL == "" {
		return nil, fmt.Errorf("SQS queue URL is required but not configured (USER_REGISTERED_QUEUE_URL)")
	}
	if cfg.Region == "" {
		return nil, fmt.Errorf("AWS region is required but not configured")
	}

	log.WithFields(log.Fields{
		"region":   cfg.Region,
		"queueURL": cfg.UserRegisteredQueueURL,
	}).Info("Creating SQS consumer")

	// Load AWS configuration
	awsCfg, err := awsconfig.LoadDefaultConfig(context.Background(),
		awsconfig.WithRegion(cfg.Region),
	)
	if err != nil {
		return nil, fmt.Errorf("failed to load AWS config: %w", err)
	}

	client := sqs.NewFromConfig(awsCfg)

	ctx, cancel := context.WithCancel(context.Background())

	consumer := &Consumer{
		client:            client,
		queueURL:          cfg.UserRegisteredQueueURL,
		serviceName:       serviceName,
		handlers:          make(map[string]MessageHandler),
		maxConcurrency:    cfg.MaxConcurrency,
		visibilityTimeout: int32(cfg.VisibilityTimeout),
		waitTimeSeconds:   int32(cfg.WaitTimeSeconds),
		ctx:               ctx,
		cancel:            cancel,
	}

	if consumer.maxConcurrency <= 0 {
		consumer.maxConcurrency = 10
	}
	if consumer.visibilityTimeout <= 0 {
		consumer.visibilityTimeout = 30
	}
	if consumer.waitTimeSeconds <= 0 {
		consumer.waitTimeSeconds = 20
	}

	log.WithFields(log.Fields{
		"region":            cfg.Region,
		"queueURL":          cfg.UserRegisteredQueueURL,
		"maxConcurrency":    consumer.maxConcurrency,
		"visibilityTimeout": consumer.visibilityTimeout,
		"waitTimeSeconds":   consumer.waitTimeSeconds,
	}).Info("SQS consumer created successfully")

	return consumer, nil
}

// RegisterHandler registers a message handler for a specific topic.
// The handler must implement aws.MessageHandler interface.
// Logs a fatal error if the handler type is invalid - this is a programming error
// that should be caught during development, not at runtime with production data.
func (c *Consumer) RegisterHandler(handler interface{}) {
	// First check if it's already a wrapped MessageHandler
	if h, ok := handler.(MessageHandler); ok {
		c.handlers[h.Topic()] = h
		log.WithFields(log.Fields{"topic": h.Topic()}).Info("Registered SQS message handler")
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
		}).Fatal("RegisterHandler requires handler with OperationName() and Topic() methods")
	}

	// Wrap the CloudEventMessageHandler with AWS middleware
	var wrapped MessageHandler
	wrapped = c.wrapCloudEventHandler(handler, operationName)

	if wrapped == nil {
		log.WithFields(log.Fields{
			"handlerType": fmt.Sprintf("%T", handler),
		}).Fatal("RegisterHandler received unsupported CloudEventMessageHandler type")
	}

	c.handlers[topic] = wrapped
	log.WithFields(log.Fields{"topic": topic}).Info("Registered SQS message handler")
}

// wrapCloudEventHandler wraps a CloudEventMessageHandler with AWS middleware.
// This uses a type switch to handle known event types.
func (c *Consumer) wrapCloudEventHandler(handler interface{}, operationName string) MessageHandler {
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
	if len(c.handlers) == 0 {
		return fmt.Errorf("no topics registered")
	}

	log.WithFields(log.Fields{
		"queueURL": c.queueURL,
		"topics":   c.getTopics(),
	}).Info("Starting SQS consumer")

	// Create worker pool
	semaphore := make(chan struct{}, c.maxConcurrency)

	// Start message polling goroutine
	c.wg.Add(1)
	go func() {
		defer c.wg.Done()
		c.pollMessages(semaphore)
	}()

	log.WithContext(c.ctx).Info("SQS consumer ready")

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
	log.WithContext(c.ctx).Info("Stopping SQS consumer...")
	c.cancel()

	// Wait for all message processing to complete
	c.wg.Wait()

	log.WithContext(c.ctx).Info("SQS consumer stopped")
	return nil
}

// pollMessages continuously polls SQS for messages
func (c *Consumer) pollMessages(semaphore chan struct{}) {
	for {
		select {
		case <-c.ctx.Done():
			log.WithContext(c.ctx).Info("SQS consumer polling stopped")
			return
		default:
			// Receive messages from SQS
			output, err := c.client.ReceiveMessage(c.ctx, &sqs.ReceiveMessageInput{
				QueueUrl:            aws.String(c.queueURL),
				MaxNumberOfMessages: 10,
				WaitTimeSeconds:     c.waitTimeSeconds,
				VisibilityTimeout:   c.visibilityTimeout,
				MessageAttributeNames: []string{
					"All",
				},
			})

			if err != nil {
				log.WithContext(c.ctx).WithFields(log.Fields{
					"error": err,
				}).Error("Failed to receive messages from SQS")
				time.Sleep(5 * time.Second)
				continue
			}

			// Process each message
			for _, message := range output.Messages {
				message := message // Capture loop variable

				// Acquire semaphore slot
				semaphore <- struct{}{}

				c.wg.Add(1)
				go func() {
					defer c.wg.Done()
					defer func() { <-semaphore }()

					if err := c.processMessage(&message); err != nil {
						log.WithContext(c.ctx).WithFields(log.Fields{
							"error":     err,
							"messageId": *message.MessageId,
						}).Error("Failed to process SQS message")
					} else {
						// Delete message after successful processing
						c.deleteMessage(&message)
					}
				}()
			}
		}
	}
}

// processMessage processes a single SQS message
// This method is now simplified - all middleware (DSM, tracing, CloudEvent parsing)
// is handled by the MessagingHandler wrapper
func (c *Consumer) processMessage(message *types.Message) error {
	ctx := c.ctx

	// Parse EventBridge envelope
	var eventBridgeEvent EventBridgeEnvelope
	if err := json.Unmarshal([]byte(*message.Body), &eventBridgeEvent); err != nil {
		return fmt.Errorf("failed to parse EventBridge envelope: %w", err)
	}

	log.WithContext(ctx).WithFields(log.Fields{
		"detailType": eventBridgeEvent.DetailType,
		"source":     eventBridgeEvent.Source,
		"messageId":  *message.MessageId,
	}).Debug("Received EventBridge event from SQS")

	// Extract Datadog headers directly from JSON without full CloudEvent parsing
	// This avoids double-parsing - the middleware will parse the full CloudEvent later
	ddHeaders := extractDatadogHeadersFromJSON(eventBridgeEvent.Detail)

	// Find handler for this detail type (maps to topic)
	detailType := eventBridgeEvent.DetailType
	handler, exists := c.handlers[detailType]
	if !exists {
		log.WithContext(ctx).WithFields(log.Fields{
			"detailType": detailType,
		}).Warn("No handler registered for detail type")
		return nil // Not an error - just skip
	}

	// Create SQS message wrapper with CloudEvent JSON body
	// The handler wrapper will parse this and apply all middleware
	sqsMessage := &SQSMessage{
		Body:       eventBridgeEvent.Detail, // CloudEvent JSON string
		Attributes: ddHeaders,               // Datadog headers for DSM/tracing
		Topic:      detailType,
		RawMessage: *message,
	}

	// Process message with handler - middleware is applied inside the handler
	if err := handler.Handle(ctx, sqsMessage); err != nil {
		return fmt.Errorf("handler error: %w", err)
	}

	log.WithContext(ctx).WithFields(log.Fields{
		"detailType": detailType,
		"messageId":  *message.MessageId,
	}).Debug("Message processed successfully")

	return nil
}

// deleteMessage deletes a message from SQS after successful processing
func (c *Consumer) deleteMessage(message *types.Message) {
	_, err := c.client.DeleteMessage(c.ctx, &sqs.DeleteMessageInput{
		QueueUrl:      aws.String(c.queueURL),
		ReceiptHandle: message.ReceiptHandle,
	})

	if err != nil {
		log.WithContext(c.ctx).WithFields(log.Fields{
			"error":     err,
			"messageId": *message.MessageId,
		}).Error("Failed to delete message from SQS")
	} else {
		log.WithContext(c.ctx).WithFields(log.Fields{
			"messageId": *message.MessageId,
		}).Debug("Message deleted from SQS")
	}
}

// getTopics returns the list of registered topics
func (c *Consumer) getTopics() []string {
	topics := make([]string, 0, len(c.handlers))
	for topic := range c.handlers {
		topics = append(topics, topic)
	}
	return topics
}

// EventBridgeEnvelope represents the EventBridge event structure received via SQS
type EventBridgeEnvelope struct {
	Version    string                 `json:"version"`
	ID         string                 `json:"id"`
	DetailType string                 `json:"detail-type"`
	Source     string                 `json:"source"`
	Time       string                 `json:"time"`
	Region     string                 `json:"region"`
	Resources  []string               `json:"resources"`
	Detail     string                 `json:"detail"` // CloudEvent JSON as string
	Account    string                 `json:"account"`
	Metadata   map[string]interface{} `json:"metadata,omitempty"`
}

// extractDatadogHeadersFromJSON efficiently extracts only the "datadog" field from CloudEvent JSON
// without parsing the entire CloudEvent structure. This avoids double-parsing since the
// middleware will parse the full CloudEvent later.
func extractDatadogHeadersFromJSON(cloudEventJSON string) map[string]string {
	// Parse just enough to get the datadog field
	var partial struct {
		Datadog map[string]interface{} `json:"datadog"`
	}

	if err := json.Unmarshal([]byte(cloudEventJSON), &partial); err != nil {
		// If parsing fails, return empty map - middleware will handle it
		return make(map[string]string)
	}

	// Convert interface{} values to strings
	headers := make(map[string]string)
	for k, v := range partial.Datadog {
		if str, ok := v.(string); ok {
			headers[k] = str
		}
	}

	return headers
}
