package errors

import (
	"errors"
	"net/http"
	"testing"

	"github.com/stretchr/testify/assert"
)

func TestServiceError_Error(t *testing.T) {
	tests := []struct {
		name     string
		err      *ServiceError
		expected string
	}{
		{
			name: "error with cause",
			err: &ServiceError{
				Code:    400,
				Message: "validation failed",
				Cause:   errors.New("field is required"),
			},
			expected: "validation failed: field is required",
		},
		{
			name: "error without cause",
			err: &ServiceError{
				Code:    404,
				Message: "resource not found",
				Cause:   nil,
			},
			expected: "resource not found",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := tt.err.Error()
			assert.Equal(t, tt.expected, result)
		})
	}
}

func TestNewServiceError(t *testing.T) {
	errCause := errors.New("underlying error")
	err := NewServiceError(500, "internal error", errCause)

	assert.Equal(t, 500, err.Code)
	assert.Equal(t, "internal error", err.Message)
	assert.Equal(t, errCause, err.Cause)
}

func TestNewBadRequestError(t *testing.T) {
	cause := ErrInvalidUserID
	err := NewBadRequestError("user ID is required", cause)

	assert.Equal(t, http.StatusBadRequest, err.Code)
	assert.Equal(t, "user ID is required", err.Message)
	assert.Equal(t, cause, err.Cause)
	assert.Contains(t, err.Error(), "user ID is required")
	assert.Contains(t, err.Error(), "invalid user ID")
}

func TestNewNotFoundError(t *testing.T) {
	cause := ErrStickerNotFound
	err := NewNotFoundError("sticker not found", cause)

	assert.Equal(t, http.StatusNotFound, err.Code)
	assert.Equal(t, "sticker not found", err.Message)
	assert.Equal(t, cause, err.Cause)
}

func TestNewConflictError(t *testing.T) {
	cause := ErrDuplicateAssignment
	err := NewConflictError("duplicate assignment", cause)

	assert.Equal(t, http.StatusConflict, err.Code)
	assert.Equal(t, "duplicate assignment", err.Message)
	assert.Equal(t, cause, err.Cause)
}

func TestNewInternalServerError(t *testing.T) {
	errCause := errors.New("database connection failed")
	err := NewInternalServerError("internal server error", errCause)

	assert.Equal(t, http.StatusInternalServerError, err.Code)
	assert.Equal(t, "internal server error", err.Message)
	assert.Equal(t, errCause, err.Cause)
}

func TestNewServiceUnavailableError(t *testing.T) {
	cause := ErrCatalogueUnavailable
	err := NewServiceUnavailableError("service unavailable", cause)

	assert.Equal(t, http.StatusServiceUnavailable, err.Code)
	assert.Equal(t, "service unavailable", err.Message)
	assert.Equal(t, cause, err.Cause)
}

func TestNewUnprocessableEntityError(t *testing.T) {
	cause := ErrStickerNotFound
	err := NewUnprocessableEntityError("sticker validation failed", cause)

	assert.Equal(t, http.StatusUnprocessableEntity, err.Code)
	assert.Equal(t, "sticker validation failed", err.Message)
	assert.Equal(t, cause, err.Cause)
}

func TestDomainErrors(t *testing.T) {
	// Test that domain errors have expected messages
	assert.Equal(t, "sticker not found", ErrStickerNotFound.Error())
	assert.Equal(t, "assignment not found", ErrAssignmentNotFound.Error())
	assert.Equal(t, "duplicate assignment", ErrDuplicateAssignment.Error())
	assert.Equal(t, "sticker not available", ErrStickerNotAvailable.Error())
	assert.Equal(t, "invalid user ID", ErrInvalidUserID.Error())
	assert.Equal(t, "invalid sticker ID", ErrInvalidStickerID.Error())
	assert.Equal(t, "catalogue service unavailable", ErrCatalogueUnavailable.Error())
}

func TestServiceError_HTTPStatusCodes(t *testing.T) {
	tests := []struct {
		name           string
		errorFunc      func(string, error) *ServiceError
		expectedStatus int
	}{
		{
			name:           "BadRequest",
			errorFunc:      NewBadRequestError,
			expectedStatus: 400,
		},
		{
			name:           "NotFound",
			errorFunc:      NewNotFoundError,
			expectedStatus: 404,
		},
		{
			name:           "Conflict",
			errorFunc:      NewConflictError,
			expectedStatus: 409,
		},
		{
			name:           "UnprocessableEntity",
			errorFunc:      NewUnprocessableEntityError,
			expectedStatus: 422,
		},
		{
			name:           "InternalServerError",
			errorFunc:      NewInternalServerError,
			expectedStatus: 500,
		},
		{
			name:           "ServiceUnavailable",
			errorFunc:      NewServiceUnavailableError,
			expectedStatus: 503,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			err := tt.errorFunc("test message", nil)
			assert.Equal(t, tt.expectedStatus, err.Code)
		})
	}
}

func TestServiceError_NilCause(t *testing.T) {
	err := NewBadRequestError("test error", nil)
	assert.Equal(t, "test error", err.Error())
	assert.Nil(t, err.Cause)
}
