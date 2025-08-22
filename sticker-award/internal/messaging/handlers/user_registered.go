package handlers

import (
	"context"
	"encoding/json"
	"fmt"

	"github.com/DataDog/dd-trace-go/v2/ddtrace/ext"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/IBM/sarama"
	"go.uber.org/zap"

	"github.com/datadog/stickerlandia/sticker-award/internal/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events/consumed"
)

const TopicUserRegistered = "users.userRegistered.v1"

// WelcomeStickerAssigner defines the interface for assigning welcome stickers
type WelcomeStickerAssigner interface {
	AssignWelcomeSticker(ctx context.Context, accountID string) error
}

// UserRegisteredHandler handles user registered events
type UserRegisteredHandler struct {
	assigner WelcomeStickerAssigner
	logger   *zap.SugaredLogger
}

// NewUserRegisteredHandler creates a new UserRegisteredHandler
func NewUserRegisteredHandler(assigner WelcomeStickerAssigner, logger *zap.SugaredLogger) *UserRegisteredHandler {
	return &UserRegisteredHandler{
		assigner: assigner,
		logger:   logger,
	}
}

// Topic returns the topic this handler processes
func (h *UserRegisteredHandler) Topic() string {
	return TopicUserRegistered
}

// Handle processes a user registered event
func (h *UserRegisteredHandler) Handle(ctx context.Context, message *sarama.ConsumerMessage) error {
	h.logger.Infow("Processing user registered event",
		"topic", message.Topic,
		"partition", message.Partition,
		"offset", message.Offset,
	)

	// Extract span links from context
	spanLinks := messaging.GetSpanLinksFromContext(ctx)

	// Create span options including span links
	spanOpts := []tracer.StartSpanOption{
		tracer.SpanType(ext.SpanTypeMessageConsumer),
		tracer.ServiceName("sticker-award"),
		tracer.ResourceName(message.Topic),
		tracer.Tag("messaging.operation.name", "process"),
		tracer.Tag("messaging.operation.type", "process"),
		tracer.Tag("messaging.system", "kafka"),
		tracer.Tag("messaging.message.envelope.size", len(message.Value)),
	}

	// Add span links if available - this must be done when creating the span
	if len(spanLinks) > 0 {
		spanOpts = append(spanOpts, tracer.WithSpanLinks(spanLinks))
		h.logger.Infow("Creating processing span with span links",
			"span_links_count", len(spanLinks),
			"topic", message.Topic)
	}

	// Start a new trace for message processing with span links
	// Use a new root span to separate from the consumer span
	span, ctx := tracer.StartSpanFromContext(context.Background(), "process users.userRegistered.v1", spanOpts...)
	defer span.Finish()

	// Debug: log the raw message content
	h.logger.Infow("Raw message received", "value", string(message.Value))

	// First try parsing as direct event (current format from user-management)
	var event consumed.UserRegisteredEvent
	if err := json.Unmarshal(message.Value, &event); err != nil {
		h.logger.Infow("Direct event parsing failed, trying CloudEvent format", "error", err.Error())

		// Fallback: try parsing as CloudEvent wrapper
		var cloudEvent events.CloudEvent[consumed.UserRegisteredEvent]
		if fallbackErr := json.Unmarshal(message.Value, &cloudEvent); fallbackErr != nil {
			span.SetTag("error", true)
			span.SetTag("error.msg", err.Error())
			h.logger.Errorw("Failed to parse user registered event as direct event or CloudEvent",
				"directEventError", err, "cloudEventError", fallbackErr, "rawMessage", string(message.Value))
			return fmt.Errorf("failed to parse user registered event: %w", err)
		}

		// Validate CloudEvent has data
		if cloudEvent.Data.AccountID == "" {
			span.SetTag("error", true)
			span.SetTag("error.msg", "CloudEvent has empty data")
			h.logger.Errorw("CloudEvent parsed successfully but contains empty data",
				"cloudEvent", cloudEvent, "rawMessage", string(message.Value))
			return fmt.Errorf("CloudEvent contains no valid user registration data")
		}

		event = cloudEvent.Data
		h.logger.Infow("Successfully parsed as CloudEvent", "accountId", event.AccountID, "eventData", event)
	} else {
		h.logger.Infow("Successfully parsed as direct event", "accountId", event.AccountID)
	}

	// Validate event
	if event.AccountID == "" {
		return fmt.Errorf("accountId is required in user registered event")
	}

	if event.EventName != "users.userRegistered.v1" {
		h.logger.Warnw("Unexpected event name",
			"expected", "users.userRegistered.v1",
			"actual", event.EventName,
		)
	}

	h.logger.Infow("Assigning welcome sticker to new user",
		"account_id", event.AccountID,
		"event_version", event.EventVersion,
	)

	// Assign welcome sticker
	if err := h.assigner.AssignWelcomeSticker(ctx, event.AccountID); err != nil {
		return fmt.Errorf("failed to assign welcome sticker to user %s: %w", event.AccountID, err)
	}

	h.logger.Infow("Successfully assigned welcome sticker",
		"account_id", event.AccountID,
	)

	return nil
}
