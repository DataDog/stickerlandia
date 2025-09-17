// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package dto

import (
	"time"
)

// AssignStickerRequest represents the request to assign a sticker to a user
type AssignStickerRequest struct {
	StickerID string  `json:"stickerId" validate:"required"`
	Reason    *string `json:"reason,omitempty" validate:"omitempty,max=500"`
}

// StickerAssignmentDTO represents a single sticker assignment
type StickerAssignmentDTO struct {
	StickerID  string    `json:"stickerId"`
	AssignedAt time.Time `json:"assignedAt"`
	Reason     *string   `json:"reason,omitempty"`
}

// UserStickersResponse represents the response for getting user's stickers
type UserStickersResponse struct {
	UserID   string                  `json:"userId"`
	Stickers []*StickerAssignmentDTO `json:"stickers"`
}

// StickerAssignmentResponse represents the response after assigning a sticker
type StickerAssignmentResponse struct {
	UserID     string    `json:"userId"`
	StickerID  string    `json:"stickerId"`
	AssignedAt time.Time `json:"assignedAt"`
}

// StickerRemovalResponse represents the response after removing a sticker assignment
type StickerRemovalResponse struct {
	UserID    string    `json:"userId"`
	StickerID string    `json:"stickerId"`
	RemovedAt time.Time `json:"removedAt"`
}
