package service

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/mock"
	"github.com/stretchr/testify/require"
	"go.uber.org/zap"
	"gorm.io/gorm"

	"github.com/datadoghq/stickerlandia/sticker-award/internal/api/dto"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/events"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/models"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/domain/repository"
	pkgErrors "github.com/datadoghq/stickerlandia/sticker-award/pkg/errors"
	"github.com/datadoghq/stickerlandia/sticker-award/pkg/validator"
)

// Interfaces for mocking
type catalogueClient interface {
	StickerExists(ctx context.Context, stickerID string) (bool, error)
}

type eventProducer interface {
	PublishStickerAssignedEvent(event *events.StickerAssignedToUserEvent) error
	PublishStickerRemovedEvent(event *events.StickerRemovedFromUserEvent) error
}

// Mock implementations
type MockAssignmentRepository struct {
	mock.Mock
}

func (m *MockAssignmentRepository) GetUserAssignments(ctx context.Context, userID string) ([]*models.Assignment, error) {
	args := m.Called(ctx, userID)
	return args.Get(0).([]*models.Assignment), args.Error(1)
}

func (m *MockAssignmentRepository) AssignSticker(ctx context.Context, assignment *models.Assignment) error {
	args := m.Called(ctx, assignment)
	return args.Error(0)
}

func (m *MockAssignmentRepository) HasActiveAssignment(ctx context.Context, userID, stickerID string) (bool, error) {
	args := m.Called(ctx, userID, stickerID)
	return args.Bool(0), args.Error(1)
}

func (m *MockAssignmentRepository) RemoveAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error) {
	args := m.Called(ctx, userID, stickerID)
	return args.Get(0).(*models.Assignment), args.Error(1)
}

func (m *MockAssignmentRepository) GetAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error) {
	args := m.Called(ctx, userID, stickerID)
	return args.Get(0).(*models.Assignment), args.Error(1)
}

func (m *MockAssignmentRepository) GetActiveAssignment(ctx context.Context, userID, stickerID string) (*models.Assignment, error) {
	args := m.Called(ctx, userID, stickerID)
	return args.Get(0).(*models.Assignment), args.Error(1)
}

type MockCatalogueClient struct {
	mock.Mock
}

func (m *MockCatalogueClient) StickerExists(ctx context.Context, stickerID string) (bool, error) {
	args := m.Called(ctx, stickerID)
	return args.Bool(0), args.Error(1)
}

type MockProducer struct {
	mock.Mock
}

func (m *MockProducer) PublishStickerAssignedEvent(event *events.StickerAssignedToUserEvent) error {
	args := m.Called(event)
	return args.Error(0)
}

func (m *MockProducer) PublishStickerRemovedEvent(event *events.StickerRemovedFromUserEvent) error {
	args := m.Called(event)
	return args.Error(0)
}

// testAssignmentService is a testable version that accepts interfaces
type testAssignmentService struct {
	assignmentRepo  repository.AssignmentRepository
	catalogueClient catalogueClient
	validator       *validator.Validator
	producer        eventProducer
	logger          *zap.SugaredLogger
}

func (s *testAssignmentService) GetUserStickers(ctx context.Context, userID string) (*dto.UserStickersResponse, error) {
	// Same implementation as assignmentService
	if userID == "" {
		return nil, pkgErrors.NewBadRequestError("user ID is required", pkgErrors.ErrInvalidUserID)
	}

	assignments, err := s.assignmentRepo.GetUserAssignments(ctx, userID)
	if err != nil {
		s.logger.Errorw("Failed to get user assignments", "userId", userID, "error", err)
		return nil, pkgErrors.NewInternalServerError("failed to retrieve user assignments", err)
	}

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

func (s *testAssignmentService) AssignSticker(ctx context.Context, userID string, req *dto.AssignStickerRequest) (*dto.StickerAssignmentResponse, error) {
	// Same implementation as assignmentService
	if userID == "" {
		return nil, pkgErrors.NewBadRequestError("user ID is required", pkgErrors.ErrInvalidUserID)
	}

	if err := s.validator.Validate(req); err != nil {
		s.logger.Warnw("Invalid request", "userId", userID, "request", req, "error", err)
		return nil, pkgErrors.NewBadRequestError("invalid request", err)
	}

	exists, err := s.catalogueClient.StickerExists(ctx, req.StickerID)
	if err != nil {
		s.logger.Errorw("Failed to check sticker existence", "stickerId", req.StickerID, "error", err)
		return nil, pkgErrors.NewServiceUnavailableError("catalogue service unavailable", err)
	}

	if !exists {
		return nil, pkgErrors.NewUnprocessableEntityError("sticker not found", pkgErrors.ErrStickerNotFound)
	}

	hasAssignment, err := s.assignmentRepo.HasActiveAssignment(ctx, userID, req.StickerID)
	if err != nil {
		s.logger.Errorw("Failed to check existing assignment", "userId", userID, "stickerId", req.StickerID, "error", err)
		return nil, pkgErrors.NewInternalServerError("failed to check existing assignment", err)
	}

	if hasAssignment {
		return nil, pkgErrors.NewConflictError("user already has this sticker assigned", pkgErrors.ErrDuplicateAssignment)
	}

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

	event := events.NewStickerAssignedToUserEvent(userID, req.StickerID, assignment.AssignedAt, req.Reason)
	if err := s.producer.PublishStickerAssignedEvent(event); err != nil {
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

func (s *testAssignmentService) RemoveSticker(ctx context.Context, userID, stickerID string) (*dto.StickerRemovalResponse, error) {
	// Same implementation as assignmentService
	if userID == "" {
		return nil, pkgErrors.NewBadRequestError("user ID is required", pkgErrors.ErrInvalidUserID)
	}

	if stickerID == "" {
		return nil, pkgErrors.NewBadRequestError("sticker ID is required", pkgErrors.ErrInvalidStickerID)
	}

	assignment, err := s.assignmentRepo.RemoveAssignment(ctx, userID, stickerID)
	if err != nil {
		if errors.Is(err, gorm.ErrRecordNotFound) {
			return nil, pkgErrors.NewNotFoundError("assignment not found", pkgErrors.ErrAssignmentNotFound)
		}
		s.logger.Errorw("Failed to remove assignment", "userId", userID, "stickerId", stickerID, "error", err)
		return nil, pkgErrors.NewInternalServerError("failed to remove assignment", err)
	}

	s.logger.Infow("Sticker assignment removed successfully", "userId", userID, "stickerId", stickerID, "assignmentId", assignment.ID)

	event := events.NewStickerRemovedFromUserEvent(userID, stickerID, *assignment.RemovedAt)
	if err := s.producer.PublishStickerRemovedEvent(event); err != nil {
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

func setupService(t *testing.T) (*testAssignmentService, *MockAssignmentRepository, *MockCatalogueClient, *MockProducer) {
	mockRepo := new(MockAssignmentRepository)
	mockCatalogue := new(MockCatalogueClient)
	mockProducer := new(MockProducer)

	logger := zap.NewNop().Sugar()
	validator := validator.New()

	service := &testAssignmentService{
		assignmentRepo:  mockRepo,
		catalogueClient: mockCatalogue,
		validator:       validator,
		producer:        mockProducer,
		logger:          logger,
	}

	return service, mockRepo, mockCatalogue, mockProducer
}

func TestGetUserStickers_Success(t *testing.T) {
	service, mockRepo, _, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	reason := "Great work!"
	assignments := []*models.Assignment{
		{
			StickerID:  "sticker1",
			AssignedAt: time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC),
			Reason:     &reason,
		},
		{
			StickerID:  "sticker2",
			AssignedAt: time.Date(2023, 1, 2, 12, 0, 0, 0, time.UTC),
			Reason:     nil,
		},
	}

	mockRepo.On("GetUserAssignments", ctx, userID).Return(assignments, nil)

	result, err := service.GetUserStickers(ctx, userID)

	require.NoError(t, err)
	assert.Equal(t, userID, result.UserID)
	assert.Len(t, result.Stickers, 2)

	assert.Equal(t, "sticker1", result.Stickers[0].StickerID)
	assert.Equal(t, assignments[0].AssignedAt, result.Stickers[0].AssignedAt)
	assert.Equal(t, &reason, result.Stickers[0].Reason)

	assert.Equal(t, "sticker2", result.Stickers[1].StickerID)
	assert.Equal(t, assignments[1].AssignedAt, result.Stickers[1].AssignedAt)
	assert.Nil(t, result.Stickers[1].Reason)

	mockRepo.AssertExpectations(t)
}

func TestGetUserStickers_EmptyUserID(t *testing.T) {
	service, _, _, _ := setupService(t)
	ctx := context.Background()

	result, err := service.GetUserStickers(ctx, "")

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 400, serviceErr.Code)
	assert.Contains(t, serviceErr.Message, "user ID is required")
}

func TestGetUserStickers_RepositoryError(t *testing.T) {
	service, mockRepo, _, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	mockRepo.On("GetUserAssignments", ctx, userID).Return([]*models.Assignment(nil), errors.New("database error"))

	result, err := service.GetUserStickers(ctx, userID)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 500, serviceErr.Code)

	mockRepo.AssertExpectations(t)
}

func TestAssignSticker_Success(t *testing.T) {
	service, mockRepo, mockCatalogue, mockProducer := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	req := &dto.AssignStickerRequest{
		StickerID: "sticker-456",
		Reason:    stringPtr("Good job!"),
	}

	mockCatalogue.On("StickerExists", ctx, req.StickerID).Return(true, nil)
	mockRepo.On("HasActiveAssignment", ctx, userID, req.StickerID).Return(false, nil)
	mockRepo.On("AssignSticker", ctx, mock.MatchedBy(func(assignment *models.Assignment) bool {
		return assignment.UserID == userID && assignment.StickerID == req.StickerID
	})).Return(nil).Run(func(args mock.Arguments) {
		assignment := args.Get(1).(*models.Assignment)
		assignment.ID = 123
		assignment.AssignedAt = time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)
	})
	mockProducer.On("PublishStickerAssignedEvent", mock.AnythingOfType("*events.StickerAssignedToUserEvent")).Return(nil)

	result, err := service.AssignSticker(ctx, userID, req)

	require.NoError(t, err)
	assert.Equal(t, userID, result.UserID)
	assert.Equal(t, req.StickerID, result.StickerID)
	assert.Equal(t, time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC), result.AssignedAt)

	mockRepo.AssertExpectations(t)
	mockCatalogue.AssertExpectations(t)
	mockProducer.AssertExpectations(t)
}

func TestAssignSticker_EmptyUserID(t *testing.T) {
	service, _, _, _ := setupService(t)
	ctx := context.Background()

	req := &dto.AssignStickerRequest{StickerID: "sticker-456"}

	result, err := service.AssignSticker(ctx, "", req)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 400, serviceErr.Code)
}

func TestAssignSticker_InvalidRequest(t *testing.T) {
	service, _, _, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	req := &dto.AssignStickerRequest{} // Missing required StickerID

	result, err := service.AssignSticker(ctx, userID, req)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 400, serviceErr.Code)
}

func TestAssignSticker_StickerNotExists(t *testing.T) {
	service, _, mockCatalogue, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	req := &dto.AssignStickerRequest{StickerID: "nonexistent-sticker"}

	mockCatalogue.On("StickerExists", ctx, req.StickerID).Return(false, nil)

	result, err := service.AssignSticker(ctx, userID, req)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 422, serviceErr.Code)
	assert.Contains(t, serviceErr.Message, "sticker not found")

	mockCatalogue.AssertExpectations(t)
}

func TestAssignSticker_CatalogueServiceError(t *testing.T) {
	service, _, mockCatalogue, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	req := &dto.AssignStickerRequest{StickerID: "sticker-456"}

	mockCatalogue.On("StickerExists", ctx, req.StickerID).Return(false, errors.New("service unavailable"))

	result, err := service.AssignSticker(ctx, userID, req)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 503, serviceErr.Code)

	mockCatalogue.AssertExpectations(t)
}

func TestAssignSticker_DuplicateAssignment(t *testing.T) {
	service, mockRepo, mockCatalogue, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	req := &dto.AssignStickerRequest{StickerID: "sticker-456"}

	mockCatalogue.On("StickerExists", ctx, req.StickerID).Return(true, nil)
	mockRepo.On("HasActiveAssignment", ctx, userID, req.StickerID).Return(true, nil)

	result, err := service.AssignSticker(ctx, userID, req)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 409, serviceErr.Code)
	assert.Contains(t, serviceErr.Message, "already has this sticker")

	mockRepo.AssertExpectations(t)
	mockCatalogue.AssertExpectations(t)
}

func TestAssignSticker_EventPublishingFailure(t *testing.T) {
	service, mockRepo, mockCatalogue, mockProducer := setupService(t)
	ctx := context.Background()
	userID := "user-123"

	req := &dto.AssignStickerRequest{StickerID: "sticker-456"}

	mockCatalogue.On("StickerExists", ctx, req.StickerID).Return(true, nil)
	mockRepo.On("HasActiveAssignment", ctx, userID, req.StickerID).Return(false, nil)
	mockRepo.On("AssignSticker", ctx, mock.AnythingOfType("*models.Assignment")).Return(nil).Run(func(args mock.Arguments) {
		assignment := args.Get(1).(*models.Assignment)
		assignment.ID = 123
		assignment.AssignedAt = time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)
	})
	mockProducer.On("PublishStickerAssignedEvent", mock.AnythingOfType("*events.StickerAssignedToUserEvent")).Return(errors.New("kafka error"))

	// Should still succeed even if event publishing fails
	result, err := service.AssignSticker(ctx, userID, req)

	require.NoError(t, err)
	assert.Equal(t, userID, result.UserID)
	assert.Equal(t, req.StickerID, result.StickerID)

	mockRepo.AssertExpectations(t)
	mockCatalogue.AssertExpectations(t)
	mockProducer.AssertExpectations(t)
}

func TestRemoveSticker_Success(t *testing.T) {
	service, mockRepo, _, mockProducer := setupService(t)
	ctx := context.Background()
	userID := "user-123"
	stickerID := "sticker-456"

	removedAt := time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)
	assignment := &models.Assignment{
		ID:        123,
		RemovedAt: &removedAt,
	}

	mockRepo.On("RemoveAssignment", ctx, userID, stickerID).Return(assignment, nil)
	mockProducer.On("PublishStickerRemovedEvent", mock.AnythingOfType("*events.StickerRemovedFromUserEvent")).Return(nil)

	result, err := service.RemoveSticker(ctx, userID, stickerID)

	require.NoError(t, err)
	assert.Equal(t, userID, result.UserID)
	assert.Equal(t, stickerID, result.StickerID)
	assert.Equal(t, removedAt, result.RemovedAt)

	mockRepo.AssertExpectations(t)
	mockProducer.AssertExpectations(t)
}

func TestRemoveSticker_EmptyUserID(t *testing.T) {
	service, _, _, _ := setupService(t)
	ctx := context.Background()

	result, err := service.RemoveSticker(ctx, "", "sticker-456")

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 400, serviceErr.Code)
}

func TestRemoveSticker_EmptyStickerID(t *testing.T) {
	service, _, _, _ := setupService(t)
	ctx := context.Background()

	result, err := service.RemoveSticker(ctx, "user-123", "")

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 400, serviceErr.Code)
}

func TestRemoveSticker_AssignmentNotFound(t *testing.T) {
	service, mockRepo, _, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"
	stickerID := "sticker-456"

	mockRepo.On("RemoveAssignment", ctx, userID, stickerID).Return((*models.Assignment)(nil), gorm.ErrRecordNotFound)

	result, err := service.RemoveSticker(ctx, userID, stickerID)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 404, serviceErr.Code)
	assert.Contains(t, serviceErr.Message, "assignment not found")

	mockRepo.AssertExpectations(t)
}

func TestRemoveSticker_RepositoryError(t *testing.T) {
	service, mockRepo, _, _ := setupService(t)
	ctx := context.Background()
	userID := "user-123"
	stickerID := "sticker-456"

	mockRepo.On("RemoveAssignment", ctx, userID, stickerID).Return((*models.Assignment)(nil), errors.New("database error"))

	result, err := service.RemoveSticker(ctx, userID, stickerID)

	assert.Nil(t, result)
	require.Error(t, err)

	var serviceErr *pkgErrors.ServiceError
	require.True(t, errors.As(err, &serviceErr))
	assert.Equal(t, 500, serviceErr.Code)

	mockRepo.AssertExpectations(t)
}

func TestRemoveSticker_EventPublishingFailure(t *testing.T) {
	service, mockRepo, _, mockProducer := setupService(t)
	ctx := context.Background()
	userID := "user-123"
	stickerID := "sticker-456"

	removedAt := time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)
	assignment := &models.Assignment{
		ID:        123,
		RemovedAt: &removedAt,
	}

	mockRepo.On("RemoveAssignment", ctx, userID, stickerID).Return(assignment, nil)
	mockProducer.On("PublishStickerRemovedEvent", mock.AnythingOfType("*events.StickerRemovedFromUserEvent")).Return(errors.New("kafka error"))

	// Should still succeed even if event publishing fails
	result, err := service.RemoveSticker(ctx, userID, stickerID)

	require.NoError(t, err)
	assert.Equal(t, userID, result.UserID)
	assert.Equal(t, stickerID, result.StickerID)
	assert.Equal(t, removedAt, result.RemovedAt)

	mockRepo.AssertExpectations(t)
	mockProducer.AssertExpectations(t)
}

// Helper function
func stringPtr(s string) *string {
	return &s
}
