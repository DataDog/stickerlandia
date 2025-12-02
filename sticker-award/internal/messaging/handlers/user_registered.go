// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package handlers

import (
	"context"
	"fmt"

	log "github.com/sirupsen/logrus"

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

// NewUserRegisteredMessageHandler creates a transport-agnostic handler for user registered events.
// The returned handler implements CloudEventMessageHandler and will be wrapped with transport-specific
// middleware (DSM, tracing, CloudEvent parsing) by the MessageConsumer when registered.
func NewUserRegisteredMessageHandler(assigner WelcomeStickerAssigner) *UserRegisteredHandler {
	return NewUserRegisteredHandler(assigner)
}

// OperationName returns the operation name for tracing
func (h *UserRegisteredHandler) OperationName() string {
	return "process " + TopicUserRegistered
}

// Topic returns the topic this handler processes
func (h *UserRegisteredHandler) Topic() string {
	return TopicUserRegistered
}

// Handle processes a user registered event with pre-parsed CloudEvent.
// This handler is transport-agnostic - it only works with the CloudEvent, not raw messages.
func (h *UserRegisteredHandler) Handle(ctx context.Context, cloudEvent events.CloudEvent[consumed.UserRegisteredEvent]) error {
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
