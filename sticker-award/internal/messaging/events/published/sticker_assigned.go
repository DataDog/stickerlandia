// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package published

import "time"

// StickerAssignedToUserEvent represents the event published when a sticker is assigned to a user
type StickerAssignedToUserEvent struct {
	EventName    string    `json:"eventName"`
	EventVersion string    `json:"eventVersion"`
	AccountID    string    `json:"accountId"`
	StickerID    string    `json:"stickerId"`
	AssignedAt   time.Time `json:"assignedAt"`
	Reason       *string   `json:"reason,omitempty"`
}

// NewStickerAssignedToUserEvent creates a new sticker assigned event
func NewStickerAssignedToUserEvent(accountID, stickerID string, assignedAt time.Time, reason *string) *StickerAssignedToUserEvent {
	return &StickerAssignedToUserEvent{
		EventName:    "StickerAssignedToUser",
		EventVersion: "v1",
		AccountID:    accountID,
		StickerID:    stickerID,
		AssignedAt:   assignedAt,
		Reason:       reason,
	}
}
