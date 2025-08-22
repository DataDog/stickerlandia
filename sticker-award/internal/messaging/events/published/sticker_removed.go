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
