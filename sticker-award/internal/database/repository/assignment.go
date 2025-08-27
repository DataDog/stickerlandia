package repository

import (
	"context"
	"time"

	"gorm.io/gorm"

	"github.com/datadog/stickerlandia/sticker-award/internal/domain/models"
	"github.com/datadog/stickerlandia/sticker-award/internal/domain/repository"
)

// assignmentRepository implements the AssignmentRepository interface using GORM
type assignmentRepository struct {
	db *gorm.DB
}

// NewAssignmentRepository creates a new assignment repository
func NewAssignmentRepository(db *gorm.DB) repository.AssignmentRepository {
	return &assignmentRepository{
		db: db,
	}
}

// GetUserAssignments retrieves all active assignments for a user
func (r *assignmentRepository) GetUserAssignments(ctx context.Context, userID string) ([]*models.Assignment, error) {
	var assignments []*models.Assignment

	err := r.db.WithContext(ctx).
		Where("user_id = ? AND removed_at IS NULL", userID).
		Order("assigned_at DESC").
		Find(&assignments).Error

	if err != nil {
		return nil, err
	}

	return assignments, nil
}

// AssignSticker creates a new sticker assignment
func (r *assignmentRepository) AssignSticker(ctx context.Context, assignment *models.Assignment) error {
	// Set timestamps
	now := time.Now().UTC()
	assignment.AssignedAt = now
	assignment.CreatedAt = now
	assignment.UpdatedAt = now

	return r.db.WithContext(ctx).Create(assignment).Error
}

// RemoveAssignment removes a sticker assignment (soft delete)
func (r *assignmentRepository) RemoveAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error) {
	var assignment models.Assignment

	// Find the active assignment
	err := r.db.WithContext(ctx).
		Where("user_id = ? AND sticker_id = ? AND removed_at IS NULL", userID, stickerID).
		First(&assignment).Error

	if err != nil {
		return nil, err
	}

	// Mark as removed
	now := time.Now().UTC()
	assignment.RemovedAt = &now
	assignment.UpdatedAt = now

	err = r.db.WithContext(ctx).Save(&assignment).Error
	if err != nil {
		return nil, err
	}

	return &assignment, nil
}

// HasActiveAssignment checks if a user has an active assignment for a sticker
func (r *assignmentRepository) HasActiveAssignment(ctx context.Context, userID, stickerID string) (bool, error) {
	var count int64

	err := r.db.WithContext(ctx).
		Model(&models.Assignment{}).
		Where("user_id = ? AND sticker_id = ? AND removed_at IS NULL", userID, stickerID).
		Count(&count).Error

	if err != nil {
		return false, err
	}

	return count > 0, nil
}

// GetAssignment retrieves a specific assignment by user and sticker ID
func (r *assignmentRepository) GetAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error) {
	var assignment models.Assignment

	err := r.db.WithContext(ctx).
		Where("user_id = ? AND sticker_id = ?", userID, stickerID).
		Order("created_at DESC").
		First(&assignment).Error

	if err != nil {
		return nil, err
	}

	return &assignment, nil
}

// GetActiveAssignment retrieves a specific active assignment by user and sticker ID
func (r *assignmentRepository) GetActiveAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error) {
	var assignment models.Assignment

	err := r.db.WithContext(ctx).
		Where("user_id = ? AND sticker_id = ? AND removed_at IS NULL", userID, stickerID).
		First(&assignment).Error

	if err != nil {
		return nil, err
	}

	return &assignment, nil
}
