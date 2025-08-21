package service

import (
	"context"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/dto"
)

// AssignmentService defines the interface for assignment business logic
type AssignmentService interface {
	// GetUserStickers retrieves all stickers assigned to a user
	GetUserStickers(ctx context.Context, userID string) (*dto.UserStickersResponse, error)

	// AssignSticker assigns a new sticker to a user
	AssignSticker(ctx context.Context, userID string, req *dto.AssignStickerRequest) (*dto.StickerAssignmentResponse, error)

	// RemoveSticker removes a sticker assignment from a user
	RemoveSticker(ctx context.Context, userID, stickerID string) (*dto.StickerRemovalResponse, error)

	// AssignWelcomeSticker assigns a welcome sticker to a newly registered user
	AssignWelcomeSticker(ctx context.Context, accountID string) error
}
