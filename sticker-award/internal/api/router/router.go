package router

import (
	"time"

	"github.com/gin-gonic/gin"
	"go.uber.org/zap"
	"gorm.io/gorm"

	"github.com/datadoghq/stickerlandia/sticker-award/internal/api/handlers"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/api/middleware"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/application/service"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/config"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/infrastructure/database/repository"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/infrastructure/external/catalogue"
	"github.com/datadoghq/stickerlandia/sticker-award/internal/infrastructure/messaging"
	"github.com/datadoghq/stickerlandia/sticker-award/pkg/validator"
)

// Setup configures and returns the Gin router with all routes and middleware
func Setup(db *gorm.DB, logger *zap.SugaredLogger, cfg *config.Config) *gin.Engine {
	// Set Gin mode based on environment
	if cfg.Logging.Level == "debug" {
		gin.SetMode(gin.DebugMode)
	} else {
		gin.SetMode(gin.ReleaseMode)
	}

	r := gin.New()

	// Global middleware
	r.Use(middleware.Logger(logger))
	r.Use(middleware.Recovery(logger))
	r.Use(middleware.CORS())

	// Initialize dependencies
	assignmentRepo := repository.NewAssignmentRepository(db)
	catalogueClient := catalogue.NewClient(cfg.Catalogue.BaseURL, time.Duration(cfg.Catalogue.Timeout)*time.Second)
	validator := validator.New()

	// Initialize Kafka producer
	producer, err := messaging.NewProducer(&cfg.Kafka, logger)
	if err != nil {
		logger.Fatalw("Failed to create Kafka producer", "error", err)
	}

	assignmentService := service.NewAssignmentService(assignmentRepo, catalogueClient, validator, producer, logger)

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
