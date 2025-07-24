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
	// Check database connection
	sqlDB, err := h.db.DB()
	if err != nil {
		h.logger.Errorw("Failed to get database connection", "error", err)
		c.JSON(http.StatusServiceUnavailable, dto.ProblemDetails{
			Type:   stringPtr("about:blank"),
			Title:  stringPtr("Service Unavailable"),
			Status: intPtr(http.StatusServiceUnavailable),
			Detail: stringPtr("Database connection check failed"),
		})
		return
	}

	if err := sqlDB.Ping(); err != nil {
		h.logger.Errorw("Database ping failed", "error", err)
		c.JSON(http.StatusServiceUnavailable, dto.ProblemDetails{
			Type:   stringPtr("about:blank"),
			Title:  stringPtr("Service Unavailable"),
			Status: intPtr(http.StatusServiceUnavailable),
			Detail: stringPtr("Database is not accessible"),
		})
		return
	}

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
