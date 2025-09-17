// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package errors

import (
	"errors"
	"net/http"
)

// Common domain errors
var (
	ErrStickerNotFound      = errors.New("sticker not found")
	ErrAssignmentNotFound   = errors.New("assignment not found")
	ErrDuplicateAssignment  = errors.New("duplicate assignment")
	ErrStickerNotAvailable  = errors.New("sticker not available")
	ErrInvalidUserID        = errors.New("invalid user ID")
	ErrInvalidStickerID     = errors.New("invalid sticker ID")
	ErrCatalogueUnavailable = errors.New("catalogue service unavailable")
)

// ServiceError represents a service-level error with HTTP status code
type ServiceError struct {
	Code    int
	Message string
	Cause   error
}

func (e *ServiceError) Error() string {
	if e.Cause != nil {
		return e.Message + ": " + e.Cause.Error()
	}
	return e.Message
}

// NewServiceError creates a new service error
func NewServiceError(code int, message string, cause error) *ServiceError {
	return &ServiceError{
		Code:    code,
		Message: message,
		Cause:   cause,
	}
}

// Common service errors
func NewBadRequestError(message string, cause error) *ServiceError {
	return NewServiceError(http.StatusBadRequest, message, cause)
}

func NewNotFoundError(message string, cause error) *ServiceError {
	return NewServiceError(http.StatusNotFound, message, cause)
}

func NewConflictError(message string, cause error) *ServiceError {
	return NewServiceError(http.StatusConflict, message, cause)
}

func NewInternalServerError(message string, cause error) *ServiceError {
	return NewServiceError(http.StatusInternalServerError, message, cause)
}

func NewServiceUnavailableError(message string, cause error) *ServiceError {
	return NewServiceError(http.StatusServiceUnavailable, message, cause)
}

func NewUnprocessableEntityError(message string, cause error) *ServiceError {
	return NewServiceError(http.StatusUnprocessableEntity, message, cause)
}
