package messaging

import (
	"context"
	"fmt"
	"log"
	"os"
	"os/signal"
	"sync"
	"syscall"

	ddsarama "github.com/DataDog/dd-trace-go/contrib/IBM/sarama/v2"
	"github.com/DataDog/dd-trace-go/v2/datastreams"
	"github.com/DataDog/dd-trace-go/v2/datastreams/options"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/IBM/sarama"
	"go.uber.org/zap"
)

// Consumer represents a Kafka consumer with Datadog DSM integration
type Consumer struct {
	client   sarama.ConsumerGroup
	handlers map[string]MessageHandler
	logger   *zap.SugaredLogger
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
func NewConsumer(brokers []string, groupID string, logger *zap.SugaredLogger) (*Consumer, error) {
	// Enable Sarama debug logging
	sarama.Logger = log.New(os.Stdout, "[sarama] ", log.LstdFlags)

	logger.Infow("Creating Kafka consumer",
		"brokers", brokers,
		"groupID", groupID,
		"protocol_version", "2.1.0.0")

	// Create shared Sarama configuration
	config := NewSaramaConfig("sticker-award-consumer")
	ConfigureConsumer(config)

	logger.Infow("Attempting to create Sarama consumer group",
		"brokers", brokers,
		"groupID", groupID)

	client, err := sarama.NewConsumerGroup(brokers, groupID, config)
	if err != nil {
		logger.Errorw("Failed to create Sarama consumer group",
			"brokers", brokers,
			"groupID", groupID,
			"error", err)
		return nil, fmt.Errorf("failed to create consumer group: %w", err)
	}

	ctx, cancel := context.WithCancel(context.Background())

	consumer := Consumer{
		client:   client,
		handlers: make(map[string]MessageHandler),
		logger:   logger,
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

	c.logger.Infow("Starting Kafka consumer",
		"group_id", c.groupID,
		"topics", c.topics,
		"brokers", c.brokers)

	c.wg.Add(1)
	go func() {
		defer c.wg.Done()
		for {
			select {
			case <-c.ctx.Done():
				c.logger.Info("Kafka consumer context cancelled")
				return
			default:
				if err := c.client.Consume(c.ctx, c.topics, ddsarama.WrapConsumerGroupHandler(c)); err != nil {
					c.logger.Errorw("Error from consumer", "error", err)
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
					c.logger.Errorw("Consumer error", "error", err)
				}
			case <-c.ctx.Done():
				return
			}
		}
	}()

	<-c.ready
	c.logger.Info("Kafka consumer ready")

	// Handle shutdown signals
	sigterm := make(chan os.Signal, 1)
	signal.Notify(sigterm, syscall.SIGINT, syscall.SIGTERM)

	select {
	case <-c.ctx.Done():
		c.logger.Info("Context cancelled")
	case <-sigterm:
		c.logger.Info("Shutdown signal received")
	}

	return nil
}

// Stop stops the consumer
func (c *Consumer) Stop() error {
	c.logger.Info("Stopping Kafka consumer...")
	c.cancel()

	if err := c.client.Close(); err != nil {
		c.logger.Errorw("Error closing consumer client", "error", err)
		return err
	}

	c.wg.Wait()
	c.logger.Info("Kafka consumer stopped")
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

			// Extract Datadog DSM context from message headers
			ctx := c.extractDatadogContext(session.Context(), message)

			// Get the span from context to finish it after processing
			span, _ := tracer.SpanFromContext(ctx)

			// Get handler for this topic
			handler, exists := c.handlers[message.Topic]
			if !exists {
				c.logger.Warnw("No handler registered for topic", "topic", message.Topic)
				if span != nil {
					span.SetTag("error", true)
					span.SetTag("error.msg", "no handler registered")
					span.Finish()
				}
				session.MarkMessage(message, "")
				continue
			}

			// Process message
			err := handler.Handle(ctx, message)
			if err != nil {
				c.logger.Errorw("Error handling message",
					"topic", message.Topic,
					"partition", message.Partition,
					"offset", message.Offset,
					"error", err)
				if span != nil {
					span.SetTag("error", true)
					span.SetTag("error.msg", err.Error())
				}
			} else {
				c.logger.Debugw("Message processed successfully",
					"topic", message.Topic,
					"partition", message.Partition,
					"offset", message.Offset)
			}

			// Finish the consumer span
			if span != nil {
				span.Finish()
			}

			session.MarkMessage(message, "")

		case <-session.Context().Done():
			return nil
		}
	}
}

// extractDatadogContext extracts Datadog tracing context from Kafka message headers
func (c *Consumer) extractDatadogContext(ctx context.Context, message *sarama.ConsumerMessage) context.Context {
	// Extract DSM context from message headers first
	ctx = datastreams.ExtractFromBase64Carrier(ctx, ddsarama.NewConsumerMessageCarrier(message))

	// Set manual DSM checkpoint for inbound message with service override
	ctx, _ = tracer.SetDataStreamsCheckpointWithParams(ctx, options.CheckpointParams{
		ServiceOverride: "sticker-award",
	}, "direction:in", "type:kafka", "topic:"+message.Topic, "manual_checkpoint:true")

	// Create span for consumer operation with proper messaging tags
	span, spanCtx := tracer.StartSpanFromContext(ctx, "kafka.consume",
		tracer.ServiceName("sticker-award"),
		tracer.ResourceName("consume "+message.Topic),
		tracer.Tag("kafka.topic", message.Topic),
		tracer.Tag("kafka.partition", message.Partition),
		tracer.Tag("kafka.offset", message.Offset),
		tracer.Tag("messaging.operation.name", "consume"),
		tracer.Tag("messaging.operation.type", "receive"),
		tracer.Tag("messaging.system", "kafka"),
		tracer.Tag("messaging.message.envelope.size", len(message.Value)),
		tracer.SpanType("queue"),
	)

	// Store span to finish later in the message processing
	ctx = tracer.ContextWithSpan(spanCtx, span)

	return ctx
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
