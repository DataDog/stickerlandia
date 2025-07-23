package service

import (
	"context"
	"errors"

	"go.uber.org/zap"
	"gorm.io/gorm"

	"github.com/datadoghq/stickerlandia/sticker-award/internal/api/dto"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/events"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/models"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/repository"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/service"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/infrastructure/external/catalogue"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/infrastructure/messaging"
	pkgErrors "github.com/datadoghq/stickerlandia/sticker-award/pkg/errors"
	"github.com/datadoghq/stickerlandia/sticker-award/pkg/validator"
)

// assignmentService implements the AssignmentService interface
type assignmentService struct {
	assignmentRepo  repository.AssignmentRepository
	catalogueClient *catalogue.Client
	validator       *validator.Validator
	producer        *messaging.Producer
	logger          *zap.SugaredLogger
}

// NewAssignmentService creates a new assignment service
func NewAssignmentService(
	assignmentRepo repository.AssignmentRepository,
	catalogueClient *catalogue.Client,
	validator *validator.Validator,
	producer *messaging.Producer,
	logger *zap.SugaredLogger,
) service.AssignmentService {
	return &assignmentService{
		assignmentRepo:  assignmentRepo,
		catalogueClient: catalogueClient,
		validator:       validator,
		producer:        producer,
		logger:          logger,
	}
}

// GetUserStickers retrieves all stickers assigned to a user
func (s *assignmentService) GetUserStickers(ctx context.Context, userID string) (*dto.UserStickersResponse, error) {
	if userID == "" {
		return nil, pkgErrors.NewBadRequestError("user ID is required", pkgErrors.ErrInvalidUserID)
	}

	assignments, err := s.assignmentRepo.GetUserAssignments(ctx, userID)
	if err != nil {
		s.logger.Errorw("Failed to get user assignments", "userId", userID, "error", err)
		return nil, pkgErrors.NewInternalServerError("failed to retrieve user assignments", err)
	}

	// Convert to DTOs
	stickerDTOs := make([]*dto.StickerAssignmentDTO, len(assignments))
	for i, assignment := range assignments {
		stickerDTOs[i] = &dto.StickerAssignmentDTO{
			StickerID:  assignment.StickerID,
			AssignedAt: assignment.AssignedAt,
			Reason:     assignment.Reason,
		}
	}

	return &dto.UserStickersResponse{
		UserID:   userID,
		Stickers: stickerDTOs,
	}, nil
}

// AssignSticker assigns a new sticker to a user
func (s *assignmentService) AssignSticker(ctx context.Context, userID string, req *dto.AssignStickerRequest) (*dto.StickerAssignmentResponse, error) {
	// Validate input
	if userID == "" {
		return nil, pkgErrors.NewBadRequestError("user ID is required", pkgErrors.ErrInvalidUserID)
	}

	if err := s.validator.Validate(req); err != nil {
		s.logger.Warnw("Invalid request", "userId", userID, "request", req, "error", err)
		return nil, pkgErrors.NewBadRequestError("invalid request", err)
	}

	// Check if sticker exists in catalogue
	exists, err := s.catalogueClient.StickerExists(ctx, req.StickerID)
	if err != nil {
		s.logger.Errorw("Failed to check sticker existence", "stickerId", req.StickerID, "error", err)
		return nil, pkgErrors.NewServiceUnavailableError("catalogue service unavailable", err)
	}

	if !exists {
		return nil, pkgErrors.NewNotFoundError("sticker not found", pkgErrors.ErrStickerNotFound)
	}

	// Check for duplicate assignment
	hasAssignment, err := s.assignmentRepo.HasActiveAssignment(ctx, userID, req.StickerID)
	if err != nil {
		s.logger.Errorw("Failed to check existing assignment", "userId", userID, "stickerId", req.StickerID, "error", err)
		return nil, pkgErrors.NewInternalServerError("failed to check existing assignment", err)
	}

	if hasAssignment {
		return nil, pkgErrors.NewConflictError("user already has this sticker assigned", pkgErrors.ErrDuplicateAssignment)
	}

	// Create assignment
	assignment := &models.Assignment{
		UserID:    userID,
		StickerID: req.StickerID,
		Reason:    req.Reason,
	}

	if err := s.assignmentRepo.AssignSticker(ctx, assignment); err != nil {
		s.logger.Errorw("Failed to create assignment", "userId", userID, "stickerId", req.StickerID, "error", err)
		return nil, pkgErrors.NewInternalServerError("failed to create assignment", err)
	}

	s.logger.Infow("Sticker assigned successfully", "userId", userID, "stickerId", req.StickerID, "assignmentId", assignment.ID)

	// Publish assignment event
	event := events.NewStickerAssignedToUserEvent(userID, req.StickerID, assignment.AssignedAt, req.Reason)
	if err := s.producer.PublishStickerAssignedEvent(event); err != nil {
		// Log the error but don't fail the request - the assignment was successful
		s.logger.Errorw("Failed to publish sticker assigned event",
			"userId", userID,
			"stickerId", req.StickerID,
			"assignmentId", assignment.ID,
			"error", err)
	}

	return &dto.StickerAssignmentResponse{
		UserID:     userID,
		StickerID:  req.StickerID,
		AssignedAt: assignment.AssignedAt,
	}, nil
}

// RemoveSticker removes a sticker assignment from a user
func (s *assignmentService) RemoveSticker(ctx context.Context, userID, stickerID string) (*dto.StickerRemovalResponse, error) {
	// Validate input
	if userID == "" {
		return nil, pkgErrors.NewBadRequestError("user ID is required", pkgErrors.ErrInvalidUserID)
	}

	if stickerID == "" {
		return nil, pkgErrors.NewBadRequestError("sticker ID is required", pkgErrors.ErrInvalidStickerID)
	}

	// Remove assignment
	assignment, err := s.assignmentRepo.RemoveAssignment(ctx, userID, stickerID)
	if err != nil {
		if errors.Is(err, gorm.ErrRecordNotFound) {
			return nil, pkgErrors.NewNotFoundError("assignment not found", pkgErrors.ErrAssignmentNotFound)
		}
		s.logger.Errorw("Failed to remove assignment", "userId", userID, "stickerId", stickerID, "error", err)
		return nil, pkgErrors.NewInternalServerError("failed to remove assignment", err)
	}

	s.logger.Infow("Sticker assignment removed successfully", "userId", userID, "stickerId", stickerID, "assignmentId", assignment.ID)

	// Publish removal event
	event := events.NewStickerRemovedFromUserEvent(userID, stickerID, *assignment.RemovedAt)
	if err := s.producer.PublishStickerRemovedEvent(event); err != nil {
		// Log the error but don't fail the request - the removal was successful
		s.logger.Errorw("Failed to publish sticker removed event",
			"userId", userID,
			"stickerId", stickerID,
			"assignmentId", assignment.ID,
			"error", err)
	}

	return &dto.StickerRemovalResponse{
		UserID:    userID,
		StickerID: stickerID,
		RemovedAt: *assignment.RemovedAt,
	}, nil
}
