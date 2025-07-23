package events

import "time"

// DomainEvent represents the base structure for all domain events
type DomainEvent struct {
	EventName    string `json:"eventName"`
	EventVersion string `json:"eventVersion"`
}

// StickerAssignedToUserEvent represents the event published when a sticker is assigned to a user
type StickerAssignedToUserEvent struct {
	DomainEvent
	AccountID  string    `json:"accountId"`
	StickerID  string    `json:"stickerId"`
	AssignedAt time.Time `json:"assignedAt"`
	Reason     *string   `json:"reason,omitempty"`
}

// NewStickerAssignedToUserEvent creates a new sticker assigned event
func NewStickerAssignedToUserEvent(accountID, stickerID string, assignedAt time.Time, reason *string) *StickerAssignedToUserEvent {
	return &StickerAssignedToUserEvent{
		DomainEvent: DomainEvent{
			EventName:    "StickerAssignedToUser",
			EventVersion: "v1",
		},
		AccountID:  accountID,
		StickerID:  stickerID,
		AssignedAt: assignedAt,
		Reason:     reason,
	}
}

// StickerRemovedFromUserEvent represents the event published when a sticker is removed from a user
type StickerRemovedFromUserEvent struct {
	DomainEvent
	AccountID string    `json:"accountId"`
	StickerID string    `json:"stickerId"`
	RemovedAt time.Time `json:"removedAt"`
}

// NewStickerRemovedFromUserEvent creates a new sticker removed event
func NewStickerRemovedFromUserEvent(accountID, stickerID string, removedAt time.Time) *StickerRemovedFromUserEvent {
	return &StickerRemovedFromUserEvent{
		DomainEvent: DomainEvent{
			EventName:    "StickerRemovedFromUser",
			EventVersion: "v1",
		},
		AccountID: accountID,
		StickerID: stickerID,
		RemovedAt: removedAt,
	}
}

// StickerClaimedEvent represents the event published when a user claims a sticker
type StickerClaimedEvent struct {
	DomainEvent
	AccountID string    `json:"accountId"`
	StickerID string    `json:"stickerId"`
	ClaimedAt time.Time `json:"claimedAt"`
}

// NewStickerClaimedEvent creates a new sticker claimed event
func NewStickerClaimedEvent(accountID, stickerID string, claimedAt time.Time) *StickerClaimedEvent {
	return &StickerClaimedEvent{
		DomainEvent: DomainEvent{
			EventName:    "StickerClaimed",
			EventVersion: "v1",
		},
		AccountID: accountID,
		StickerID: stickerID,
		ClaimedAt: claimedAt,
	}
}
