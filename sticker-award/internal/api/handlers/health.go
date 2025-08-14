package handlers

import (
	"net/http"
	"time"

	"github.com/gin-gonic/gin"
	"go.uber.org/zap"
	"gorm.io/gorm"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/dto"
)

// HealthHandler handles health check requests
type HealthHandler struct {
	db     *gorm.DB
	logger *zap.SugaredLogger
}

// NewHealthHandler creates a new health handler
func NewHealthHandler(db *gorm.DB, logger *zap.SugaredLogger) *HealthHandler {
	return &HealthHandler{
		db:     db,
		logger: logger,
	}
}

// Handle handles the health check endpoint
func (h *HealthHandler) Handle(c *gin.Context) {
	// Simple liveness response – no DB checks to avoid extra spans
	c.JSON(http.StatusOK, dto.HealthResponse{
		Status: "healthy",
		Time:   time.Now().UTC().Format(time.RFC3339),
	})
}

// Helper functions for pointer types
func stringPtr(s string) *string {
	return &s
}

func intPtr(i int) *int {
	return &i
}
