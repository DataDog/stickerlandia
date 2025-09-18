// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package service

import (
	"context"
	"errors"
	"fmt"
	log "github.com/sirupsen/logrus"

	"gorm.io/gorm"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/dto"
	"github.com/datadog/stickerlandia/sticker-award/internal/clients/catalogue"
	"github.com/datadog/stickerlandia/sticker-award/internal/domain/models"
	"github.com/datadog/stickerlandia/sticker-award/internal/domain/repository"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events/published"
	pkgErrors "github.com/datadog/stickerlandia/sticker-award/pkg/errors"
	"github.com/datadog/stickerlandia/sticker-award/pkg/validator"
)

// Assigner defines the interface for assignment business logic
type Assigner interface {
	// GetUserStickers retrieves all stickers assigned to a user
	GetUserStickers(ctx context.Context, userID string) (*dto.UserStickersResponse, error)

	// AssignSticker assigns a new sticker to a user
	AssignSticker(ctx context.Context, userID string, req *dto.AssignStickerRequest) (*dto.StickerAssignmentResponse, error)

	// RemoveSticker removes a sticker assignment from a user
	RemoveSticker(ctx context.Context, userID, stickerID string) (*dto.StickerRemovalResponse, error)

	// AssignWelcomeSticker assigns a welcome sticker to a newly registered user
	AssignWelcomeSticker(ctx context.Context, accountID string) error
}

// Constants for special stickers
const (
	WelcomeStickerID = "sticker-001" // TODO: This should be configurable or looked up from catalogue
)

// assignmentService implements the Assigner interface
type assignmentService struct {
	assignmentRepo  repository.AssignmentRepository
	catalogueClient *catalogue.Client
	validator       *validator.Validator
	producer        *messaging.Producer
}

// NewAssigner creates a new assignment service
func NewAssigner(
	assignmentRepo repository.AssignmentRepository,
	catalogueClient *catalogue.Client,
	validator *validator.Validator,
	producer *messaging.Producer,
) Assigner {
	return &assignmentService{
		assignmentRepo:  assignmentRepo,
		catalogueClient: catalogueClient,
		validator:       validator,
		producer:        producer,
	}
}

// GetUserStickers retrieves all stickers assigned to a user
func (s *assignmentService) GetUserStickers(ctx context.Context, userID string) (*dto.UserStickersResponse, error) {
	if userID == "" {
		return nil, pkgErrors.NewBadRequestError("user ID is required", pkgErrors.ErrInvalidUserID)
	}

	assignments, err := s.assignmentRepo.GetUserAssignments(ctx, userID)
	if err != nil {
		log.WithContext(ctx).WithFields(log.Fields{
			"userID": userID,
			"error":  err,
		}).Error("Failed to get user assignments")
		return nil, pkgErrors.NewInternalServerError("failed to retrieve user assignments", err)
	}
	// Logging removed for now

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
		return nil, pkgErrors.NewBadRequestError("invalid request", err)
	}

	// Check if sticker exists in catalogue
	exists, err := s.catalogueClient.StickerExists(ctx, req.StickerID)
	if err != nil {
		return nil, pkgErrors.NewServiceUnavailableError("catalogue service unavailable", err)
	}

	if !exists {
		return nil, pkgErrors.NewUnprocessableEntityError("sticker not found", pkgErrors.ErrStickerNotFound)
	}

	// Check for duplicate assignment
	hasAssignment, err := s.assignmentRepo.HasActiveAssignment(ctx, userID, req.StickerID)
	if err != nil {
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
		return nil, pkgErrors.NewInternalServerError("failed to create assignment", err)
	}

	// Publish assignment event
	if s.producer != nil {
		event := published.NewStickerAssignedToUserEvent(userID, req.StickerID, assignment.AssignedAt, req.Reason)
		if err := s.producer.PublishStickerAssignedEvent(ctx, event); err != nil {
			// Log the error but don't fail the request - the assignment was successful
		}
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
		return nil, pkgErrors.NewInternalServerError("failed to remove assignment", err)
	}

	// Publish removal event
	if s.producer != nil {
		event := published.NewStickerRemovedFromUserEvent(userID, stickerID, *assignment.RemovedAt)
		if err := s.producer.PublishStickerRemovedEvent(ctx, event); err != nil {
			// Log the error but don't fail the request - the removal was successful
		}
	}

	return &dto.StickerRemovalResponse{
		UserID:    userID,
		StickerID: stickerID,
		RemovedAt: *assignment.RemovedAt,
	}, nil
}

// AssignWelcomeSticker assigns a welcome sticker to a newly registered user
func (s *assignmentService) AssignWelcomeSticker(ctx context.Context, accountID string) error {
	if accountID == "" {
		return pkgErrors.NewBadRequestError("account ID is required", pkgErrors.ErrInvalidUserID)
	}

	// Check if sticker exists in catalogue (optional - we could skip this for welcome sticker)
	exists, err := s.catalogueClient.StickerExists(ctx, WelcomeStickerID)
	if err != nil {
		log.WithContext(ctx).WithFields(log.Fields{
			"stickerID": WelcomeStickerID,
			"error":     err,
		}).Warn("Failed to check welcome sticker existence, proceeding anyway")
	} else if !exists {
		log.WithContext(ctx).WithFields(log.Fields{
			"stickerID": WelcomeStickerID,
		}).Warn("Welcome sticker not found in catalogue, proceeding anyway")
	}

	// Check for duplicate assignment (user might already have welcome sticker)
	hasAssignment, err := s.assignmentRepo.HasActiveAssignment(ctx, accountID, WelcomeStickerID)
	if err != nil {
		return fmt.Errorf("failed to check existing assignment: %w", err)
	}

	if hasAssignment {
		return nil // Not an error - user already has the welcome sticker
	}

	// Create assignment
	welcomeReason := "Welcome to Stickerlandia!"
	assignment := &models.Assignment{
		UserID:    accountID,
		StickerID: WelcomeStickerID,
		Reason:    &welcomeReason,
	}

	if err := s.assignmentRepo.AssignSticker(ctx, assignment); err != nil {
		return fmt.Errorf("failed to create welcome assignment: %w", err)
	}

	// Publish assignment event
	if s.producer != nil {
		event := published.NewStickerAssignedToUserEvent(accountID, WelcomeStickerID, assignment.AssignedAt, assignment.Reason)
		if err := s.producer.PublishStickerAssignedEvent(ctx, event); err != nil {
			// Log the error but don't fail the request - the assignment was successful
		}
	}

	return nil
}
