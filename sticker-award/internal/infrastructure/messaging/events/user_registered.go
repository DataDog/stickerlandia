package events

import (
	"context"
	"encoding/json"
	"fmt"

	"github.com/IBM/sarama"
	"go.uber.org/zap"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/ext"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
)

const TopicUserRegistered = "users.userRegistered.v1"

// UserRegisteredEvent represents the user registered event from user-management service
type UserRegisteredEvent struct {
	EventName    string `json:"eventName"`
	EventVersion string `json:"eventVersion"`
	AccountID    string `json:"accountId"`
}

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

	// Context with DSM already extracted in consumer - don't extract again
	// Start a span for message processing
	span, ctx := tracer.StartSpanFromContext(ctx, "process users.userRegistered.v1",
		tracer.SpanType(ext.SpanTypeMessageConsumer),
		tracer.ServiceName("sticker-award"),
		tracer.ResourceName(message.Topic),
		tracer.Tag("messaging.operation.name", "process"),
		tracer.Tag("messaging.operation.type", "process"),
		tracer.Tag("messaging.system", "kafka"),
		tracer.Tag("messaging.message.envelope.size", len(message.Value)),
	)
	defer span.Finish()

	// Parse the event
	var event UserRegisteredEvent
	if err := json.Unmarshal(message.Value, &event); err != nil {
		return fmt.Errorf("failed to unmarshal user registered event: %w", err)
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
