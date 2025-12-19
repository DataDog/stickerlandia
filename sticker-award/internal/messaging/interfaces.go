// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package messaging

import (
	"context"

	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events/published"
)

// EventPublisher defines the interface for publishing domain events.
// This abstraction allows the service to support multiple messaging transports
// (Kafka, AWS EventBridge, etc.) without changing business logic.
//
// Implementations are responsible for:
// - Wrapping events in CloudEvents format
// - Injecting Datadog trace context
// - Publishing to the transport-specific destination
// - Managing connections and resources
type EventPublisher interface {
	// PublishStickerAssignedEvent publishes an event when a sticker is assigned to a user
	PublishStickerAssignedEvent(ctx context.Context, event *published.StickerAssignedToUserEvent) error

	// PublishStickerRemovedEvent publishes an event when a sticker is removed from a user
	PublishStickerRemovedEvent(ctx context.Context, event *published.StickerRemovedFromUserEvent) error

	// PublishStickerClaimedEvent publishes an event when a user claims a sticker
	PublishStickerClaimedEvent(ctx context.Context, event *published.StickerClaimedEvent) error

	// Close releases any resources held by the publisher
	Close() error
}

// MessageConsumer defines the interface for consuming messages from a messaging transport.
// This abstraction allows the service to consume from different transports
// (Kafka consumer groups, AWS SQS queues, etc.) using the same interface.
//
// Implementations are responsible for:
// - Managing connections to the message broker/queue
// - Polling or subscribing for messages
// - Wrapping handlers with transport-specific middleware (DSM, tracing, CloudEvent parsing)
// - Managing message acknowledgment/deletion
// - Graceful shutdown
type MessageConsumer interface {
	// RegisterHandler registers a message handler for processing messages.
	// The handler parameter should be a CloudEventMessageHandler[T] that will be
	// wrapped with transport-specific middleware (DSM, tracing, CloudEvent parsing)
	// by the consumer implementation.
	//
	// The handler must also implement an OperationName() method that returns the
	// operation name for tracing (e.g., "process users.userRegistered.v1").
	RegisterHandler(handler interface{})

	// Start begins consuming messages. This is a blocking call that runs until
	// the consumer is stopped or an unrecoverable error occurs.
	// The consumer manages its own internal context.
	Start() error

	// Stop gracefully shuts down the consumer, waiting for in-flight messages to complete
	Stop() error
}

// CloudEventMessageHandler defines the interface for handlers that work with parsed CloudEvents.
// This is the shared interface used by both Kafka and AWS implementations.
//
// Business handlers implement this interface to remain transport-agnostic - they only work
// with CloudEvent[T], not raw transport messages (Kafka ConsumerMessage or AWS SQSMessage).
//
// The middleware wrappers (kafka.MessagingHandler and aws.MessagingHandler) handle:
// - DSM tracking
// - Root trace creation with span links
// - CloudEvent parsing from raw messages
// - Calling this handler with the parsed CloudEvent
type CloudEventMessageHandler[T any] interface {
	// Handle processes a parsed CloudEvent. Business logic should not depend on
	// transport-specific message formats.
	Handle(ctx context.Context, cloudEvent events.CloudEvent[T]) error

	// Topic returns the topic/detail-type this handler processes
	Topic() string
}
