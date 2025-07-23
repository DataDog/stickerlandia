package events

import (
	"time"
)

// StickerAssignedEvent represents the event published when a sticker is assigned to a user
type StickerAssignedEvent struct {
	EventName    string    `json:"eventName"`
	EventVersion string    `json:"eventVersion"`
	AccountID    string    `json:"accountId"`
	StickerID    string    `json:"stickerId"`
	AssignedAt   time.Time `json:"assignedAt"`
	Reason       *string   `json:"reason,omitempty"`
}

// NewStickerAssignedEvent creates a new StickerAssignedEvent
func NewStickerAssignedEvent(userID, stickerID string, assignedAt time.Time, reason *string) *StickerAssignedEvent {
	return &StickerAssignedEvent{
		EventName:    "StickerAssignedToUser",
		EventVersion: "v1",
		AccountID:    userID,
		StickerID:    stickerID,
		AssignedAt:   assignedAt,
		Reason:       reason,
	}
}

// StickerRemovedEvent represents the event published when a sticker assignment is removed
type StickerRemovedEvent struct {
	EventName    string    `json:"eventName"`
	EventVersion string    `json:"eventVersion"`
	AccountID    string    `json:"accountId"`
	StickerID    string    `json:"stickerId"`
	RemovedAt    time.Time `json:"removedAt"`
}

// NewStickerRemovedEvent creates a new StickerRemovedEvent
func NewStickerRemovedEvent(userID, stickerID string, removedAt time.Time) *StickerRemovedEvent {
	return &StickerRemovedEvent{
		EventName:    "StickerRemovedFromUser",
		EventVersion: "v1",
		AccountID:    userID,
		StickerID:    stickerID,
		RemovedAt:    removedAt,
	}
}

// StickerClaimedEvent represents the event published when a user claims a sticker
type StickerClaimedEvent struct {
	EventName    string    `json:"eventName"`
	EventVersion string    `json:"eventVersion"`
	AccountID    string    `json:"accountId"`
	StickerID    string    `json:"stickerId"`
	ClaimedAt    time.Time `json:"claimedAt"`
}

// NewStickerClaimedEvent creates a new StickerClaimedEvent
func NewStickerClaimedEvent(userID, stickerID string, claimedAt time.Time) *StickerClaimedEvent {
	return &StickerClaimedEvent{
		EventName:    "StickerClaimed",
		EventVersion: "v1",
		AccountID:    userID,
		StickerID:    stickerID,
		ClaimedAt:    claimedAt,
	}
}
