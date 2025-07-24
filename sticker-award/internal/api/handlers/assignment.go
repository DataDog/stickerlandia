package handlers

import (
	"errors"
	"net/http"

	"github.com/gin-gonic/gin"
	"go.uber.org/zap"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/dto"
	"github.com/datadog/stickerlandia/sticker-award/internal/domain/service"
	pkgErrors "github.com/datadog/stickerlandia/sticker-award/pkg/errors"
)

// AssignmentHandler handles sticker assignment requests
type AssignmentHandler struct {
	assignmentService service.AssignmentService
	logger            *zap.SugaredLogger
}

// NewAssignmentHandler creates a new assignment handler
func NewAssignmentHandler(assignmentService service.AssignmentService, logger *zap.SugaredLogger) *AssignmentHandler {
	return &AssignmentHandler{
		assignmentService: assignmentService,
		logger:            logger,
	}
}

// GetUserStickers handles GET /api/awards/v1/assignments/{userId}
func (h *AssignmentHandler) GetUserStickers(c *gin.Context) {
	userID := c.Param("userId")

	response, err := h.assignmentService.GetUserStickers(c.Request.Context(), userID)
	if err != nil {
		h.handleServiceError(c, err)
		return
	}

	c.JSON(http.StatusOK, response)
}

// AssignSticker handles POST /api/awards/v1/assignments/{userId}
func (h *AssignmentHandler) AssignSticker(c *gin.Context) {
	userID := c.Param("userId")

	var req dto.AssignStickerRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		h.logger.Warnw("Invalid request body", "error", err)
		c.JSON(http.StatusBadRequest, dto.ProblemDetails{
			Type:   stringPtr("about:blank"),
			Title:  stringPtr("Bad Request"),
			Status: intPtr(http.StatusBadRequest),
			Detail: stringPtr("Invalid request body"),
		})
		return
	}

	response, err := h.assignmentService.AssignSticker(c.Request.Context(), userID, &req)
	if err != nil {
		h.handleServiceError(c, err)
		return
	}

	c.JSON(http.StatusCreated, response)
}

// RemoveSticker handles DELETE /api/awards/v1/assignments/{userId}/{stickerId}
func (h *AssignmentHandler) RemoveSticker(c *gin.Context) {
	userID := c.Param("userId")
	stickerID := c.Param("stickerId")

	response, err := h.assignmentService.RemoveSticker(c.Request.Context(), userID, stickerID)
	if err != nil {
		h.handleServiceError(c, err)
		return
	}

	c.JSON(http.StatusOK, response)
}

// handleServiceError converts service errors to appropriate HTTP responses
func (h *AssignmentHandler) handleServiceError(c *gin.Context, err error) {
	var serviceErr *pkgErrors.ServiceError
	if errors.As(err, &serviceErr) {
		// Use the service error as-is
	} else {
		// Default to internal server error for unknown errors
		serviceErr = pkgErrors.NewInternalServerError("An unexpected error occurred", err)
	}

	h.logger.Errorw("Service error",
		"error", err,
		"statusCode", serviceErr.Code,
		"path", c.Request.URL.Path,
		"method", c.Request.Method,
	)

	c.JSON(serviceErr.Code, dto.ProblemDetails{
		Type:   stringPtr("about:blank"),
		Title:  stringPtr(http.StatusText(serviceErr.Code)),
		Status: intPtr(serviceErr.Code),
		Detail: stringPtr(serviceErr.Message),
	})
}
