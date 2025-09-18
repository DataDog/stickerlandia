// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package published

import "time"

// StickerClaimedEvent represents the event published when a user claims a sticker
type StickerClaimedEvent struct {
	EventName    string    `json:"eventName"`
	EventVersion string    `json:"eventVersion"`
	AccountID    string    `json:"accountId"`
	StickerID    string    `json:"stickerId"`
	ClaimedAt    time.Time `json:"claimedAt"`
}

// NewStickerClaimedEvent creates a new sticker claimed event
func NewStickerClaimedEvent(accountID, stickerID string, claimedAt time.Time) *StickerClaimedEvent {
	return &StickerClaimedEvent{
		EventName:    "StickerClaimed",
		EventVersion: "v1",
		AccountID:    accountID,
		StickerID:    stickerID,
		ClaimedAt:    claimedAt,
	}
}
