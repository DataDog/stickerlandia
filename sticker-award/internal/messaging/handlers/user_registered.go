package handlers

import (
	"context"
	"encoding/json"
	"fmt"

	"github.com/DataDog/dd-trace-go/v2/ddtrace/ext"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/IBM/sarama"
	log "github.com/sirupsen/logrus"

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
}

// NewUserRegisteredHandler creates a new UserRegisteredHandler
func NewUserRegisteredHandler(assigner WelcomeStickerAssigner) *UserRegisteredHandler {
	return &UserRegisteredHandler{
		assigner: assigner,
	}
}

// Topic returns the topic this handler processes
func (h *UserRegisteredHandler) Topic() string {
	return TopicUserRegistered
}

// Handle processes a user registered event
func (h *UserRegisteredHandler) Handle(ctx context.Context, message *sarama.ConsumerMessage) error {
	log.WithContext(ctx).WithFields(log.Fields{
		"topic":     message.Topic,
		"partition": message.Partition,
		"offset":    message.Offset,
	}).Info("Processing user registered event")

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
		log.WithContext(ctx).WithFields(log.Fields{
			"span_links_count": len(spanLinks),
			"topic":            message.Topic,
		}).Info("Creating processing span with span links")
	} else {
		log.WithContext(ctx).Warn("Couldn't find any span links")
	}

	// Start a new trace for message processing with span links
	// Use a new root span to separate from the consumer span
	span, ctx := tracer.StartSpanFromContext(context.Background(), "process users.userRegistered.v1", spanOpts...)
	defer span.Finish()

	// Parse CloudEvent
	var cloudEvent events.CloudEvent[consumed.UserRegisteredEvent]
	if err := json.Unmarshal(message.Value, &cloudEvent); err != nil {
		span.SetTag("error", true)
		span.SetTag("error.msg", err.Error())
		log.WithContext(ctx).WithFields(log.Fields{
			"error":      err.Error(),
			"rawMessage": string(message.Value),
		}).Error("Failed to parse user registered CloudEvent")
		return fmt.Errorf("failed to parse user registered CloudEvent: %w", err)
	}

	event := cloudEvent.Data
	log.WithContext(ctx).WithFields(log.Fields{
		"accountId": event.AccountID,
		"eventId":   cloudEvent.Id,
		"eventType": cloudEvent.Type,
	}).Info("Successfully parsed CloudEvent")

	// Validate event
	if event.AccountID == "" {
		return fmt.Errorf("accountId is required in user registered event")
	}

	if event.EventName != "users.userRegistered.v1" {
		log.WithContext(ctx).WithFields(log.Fields{
			"expected": "users.userRegistered.v1",
			"actual":   event.EventName,
		}).Warn("Unexpected event name")
	}

	log.WithContext(ctx).WithFields(log.Fields{
		"account_id":    event.AccountID,
		"event_version": event.EventVersion,
	}).Info("Assigning welcome sticker to new user")

	// Assign welcome sticker
	if err := h.assigner.AssignWelcomeSticker(ctx, event.AccountID); err != nil {
		return fmt.Errorf("failed to assign welcome sticker to user %s: %w", event.AccountID, err)
	}

	log.WithContext(ctx).WithFields(log.Fields{
		"account_id": event.AccountID,
	}).Info("Successfully assigned welcome sticker")

	return nil
}
