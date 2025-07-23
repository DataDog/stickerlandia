package repository

import (
	"context"

	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/models"
)

// AssignmentRepository defines the interface for assignment data operations
type AssignmentRepository interface {
	// GetUserAssignments retrieves all active assignments for a user
	GetUserAssignments(ctx context.Context, userID string) ([]*models.Assignment, error)

	// AssignSticker creates a new sticker assignment
	AssignSticker(ctx context.Context, assignment *models.Assignment) error

	// RemoveAssignment removes a sticker assignment (soft delete)
	RemoveAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error)

	// HasActiveAssignment checks if a user has an active assignment for a sticker
	HasActiveAssignment(ctx context.Context, userID, stickerID string) (bool, error)

	// GetAssignment retrieves a specific assignment by user and sticker ID
	GetAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error)

	// GetActiveAssignment retrieves a specific active assignment by user and sticker ID
	GetActiveAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error)
}
