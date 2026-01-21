// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package router

import (
	gintrace "github.com/DataDog/dd-trace-go/contrib/gin-gonic/gin/v2"
	"github.com/gin-gonic/gin"
	"gorm.io/gorm"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/handlers"
	"github.com/datadog/stickerlandia/sticker-award/internal/api/middleware"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	domainservice "github.com/datadog/stickerlandia/sticker-award/internal/domain/service"
)

// Setup configures and returns the Gin router with all routes and middleware
func Setup(db *gorm.DB, cfg *config.Config, assignmentService domainservice.Assigner, jwtValidator *middleware.JWTValidator) *gin.Engine {
	// Set Gin mode based on environment
	if cfg.Logging.Level == "debug" {
		gin.SetMode(gin.DebugMode)
	} else {
		gin.SetMode(gin.ReleaseMode)
	}

	r := gin.New()

	// Global middleware
	r.Use(gintrace.Middleware("sticker-award"))
	r.Use(middleware.Logger())
	r.Use(middleware.Recovery())
	r.Use(middleware.CORS())

	// Health check endpoint (public)
	r.GET("/health", handlers.NewHealthHandler(db).Handle)

	// API v1 routes
	v1 := r.Group("/api/awards/v1")
	{
		// Assignment routes
		assignments := v1.Group("/assignments")
		{
			assignmentHandler := handlers.NewAssignmentHandler(assignmentService)

			// All assignment endpoints require JWT authentication
			if jwtValidator != nil {
				assignments.Use(jwtValidator.JWT())
			}
			assignments.GET("/:userId", assignmentHandler.GetUserStickers)
			assignments.POST("/:userId", assignmentHandler.AssignSticker)
			assignments.DELETE("/:userId/:stickerId", assignmentHandler.RemoveSticker)
		}
	}

	return r
}
