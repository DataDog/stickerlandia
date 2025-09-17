// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

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
