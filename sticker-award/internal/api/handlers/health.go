// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package handlers

import (
	log "github.com/sirupsen/logrus"
	"net/http"
	"time"

	"github.com/gin-gonic/gin"
	"gorm.io/gorm"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/dto"
)

// HealthHandler handles health check requests
type HealthHandler struct {
	db *gorm.DB
}

// NewHealthHandler creates a new health handler
func NewHealthHandler(db *gorm.DB) *HealthHandler {
	return &HealthHandler{
		db: db,
	}
}

// Handle handles the health check endpoint
func (h *HealthHandler) Handle(c *gin.Context) {
	log.WithContext(c.Request.Context()).Info("HealthHandler.Handle")

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
