// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package published

import "time"

// StickerRemovedFromUserEvent represents the event published when a sticker is removed from a user
type StickerRemovedFromUserEvent struct {
	EventName    string    `json:"eventName"`
	EventVersion string    `json:"eventVersion"`
	AccountID    string    `json:"accountId"`
	StickerID    string    `json:"stickerId"`
	RemovedAt    time.Time `json:"removedAt"`
}

// NewStickerRemovedFromUserEvent creates a new sticker removed event
func NewStickerRemovedFromUserEvent(accountID, stickerID string, removedAt time.Time) *StickerRemovedFromUserEvent {
	return &StickerRemovedFromUserEvent{
		EventName:    "StickerRemovedFromUser",
		EventVersion: "v1",
		AccountID:    accountID,
		StickerID:    stickerID,
		RemovedAt:    removedAt,
	}
}
