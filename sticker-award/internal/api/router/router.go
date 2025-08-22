package router

import (
	gintrace "github.com/DataDog/dd-trace-go/contrib/gin-gonic/gin/v2"
	"github.com/gin-gonic/gin"
	"go.uber.org/zap"
	"gorm.io/gorm"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/handlers"
	"github.com/datadog/stickerlandia/sticker-award/internal/api/middleware"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	domainservice "github.com/datadog/stickerlandia/sticker-award/internal/domain/service"
)

// Setup configures and returns the Gin router with all routes and middleware
func Setup(db *gorm.DB, logger *zap.SugaredLogger, cfg *config.Config, assignmentService domainservice.Assigner) *gin.Engine {
	// Set Gin mode based on environment
	if cfg.Logging.Level == "debug" {
		gin.SetMode(gin.DebugMode)
	} else {
		gin.SetMode(gin.ReleaseMode)
	}

	r := gin.New()

	// Global middleware
	r.Use(gintrace.Middleware("sticker-award"))
	r.Use(middleware.Logger(logger))
	r.Use(middleware.Recovery(logger))
	r.Use(middleware.CORS())

	// Health check endpoint
	r.GET("/health", handlers.NewHealthHandler(db, logger).Handle)

	// API v1 routes
	v1 := r.Group("/api/awards/v1")
	{
		// Assignment routes
		assignments := v1.Group("/assignments")
		{
			assignmentHandler := handlers.NewAssignmentHandler(assignmentService, logger)
			assignments.GET("/:userId", assignmentHandler.GetUserStickers)
			assignments.POST("/:userId", assignmentHandler.AssignSticker)
			assignments.DELETE("/:userId/:stickerId", assignmentHandler.RemoveSticker)
		}
	}

	return r
}
