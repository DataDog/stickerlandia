package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"sync"
	"syscall"
	"time"

	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/DataDog/dd-trace-go/v2/profiler"
	"github.com/datadog/stickerlandia/sticker-award/internal/api/router"
	"github.com/datadog/stickerlandia/sticker-award/internal/application/service"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/infrastructure/database"
	"github.com/datadog/stickerlandia/sticker-award/internal/infrastructure/database/repository"
	"github.com/datadog/stickerlandia/sticker-award/internal/infrastructure/external/catalogue"
	"github.com/datadog/stickerlandia/sticker-award/internal/infrastructure/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/infrastructure/messaging/events"
	"github.com/datadog/stickerlandia/sticker-award/pkg/logger"
	"github.com/datadog/stickerlandia/sticker-award/pkg/validator"
)

func main() {
	// Initialize configuration
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("Failed to load configuration: %v", err)
	}

	// Start the DataDog tracer
	tracer.Start()
	defer tracer.Stop()

	// Setup the DataDog profiler
	err = profiler.Start(
		profiler.WithProfileTypes(
			profiler.CPUProfile,
			profiler.HeapProfile,
		),
	)
	if err != nil {
		log.Printf("Warning: Failed to start DataDog profiler: %v", err)
	} else {
		defer profiler.Stop()
	}

	// Initialize logger
	baseLogger, err := logger.New(cfg.Logging.Level, cfg.Logging.Format)
	if err != nil {
		log.Fatalf("Failed to initialize logger: %v", err)
	}
	defer baseLogger.Sync()

	logger := baseLogger.Sugar()
	logger.Infow("Starting Sticker Award Service",
		"port", cfg.Server.Port,
	)

	// Initialize database connection
	db, err := database.Connect(&cfg.Database)
	if err != nil {
		logger.Fatalw("Failed to connect to database", "error", err)
	}

	// Run database migrations
	logger.Info("Running database migrations...")
	if err := database.RunMigrations(db); err != nil {
		logger.Fatalw("Failed to run database migrations", "error", err)
	}
	logger.Info("Database migrations completed successfully")

	// Initialize services dependencies
	assignmentRepo := repository.NewAssignmentRepository(db)
	catalogueClient := catalogue.NewClient(cfg.Catalogue.BaseURL, time.Duration(cfg.Catalogue.Timeout)*time.Second)
	validator := validator.New()

	// Initialize Kafka producer
	producer, err := messaging.NewProducer(&cfg.Kafka, logger)
	if err != nil {
		logger.Fatalw("Failed to create Kafka producer", "error", err)
	}

	assignmentService := service.NewAssignmentService(assignmentRepo, catalogueClient, validator, producer, logger)

	// Initialize HTTP router
	r := router.Setup(db, logger, cfg, assignmentService)

	// Log Kafka configuration being used
	logger.Infow("Kafka configuration loaded",
		"brokers", cfg.Kafka.Brokers,
		"groupID", cfg.Kafka.GroupID,
		"producer_timeout", cfg.Kafka.ProducerTimeout)

	// Initialize Kafka consumer
	consumer, err := messaging.NewConsumer(cfg.Kafka.Brokers, cfg.Kafka.GroupID, logger)
	if err != nil {
		logger.Fatalw("Failed to create Kafka consumer", "error", err)
	}

	// Register user registered event handler
	userRegisteredHandler := events.NewUserRegisteredHandler(assignmentService, logger)
	consumer.RegisterHandler(userRegisteredHandler)

	// Create HTTP server
	srv := &http.Server{
		Addr:           fmt.Sprintf(":%s", cfg.Server.Port),
		Handler:        r,
		ReadTimeout:    15 * time.Second,
		WriteTimeout:   15 * time.Second,
		IdleTimeout:    60 * time.Second,
		MaxHeaderBytes: 1 << 20, // 1MB
	}

	var wg sync.WaitGroup

	// Start HTTP server in a goroutine
	wg.Add(1)
	go func() {
		defer wg.Done()
		logger.Infow("HTTP server starting", "addr", srv.Addr)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			logger.Errorw("HTTP server error", "error", err)
		}
	}()

	// Start Kafka consumer in a goroutine
	wg.Add(1)
	go func() {
		defer wg.Done()
		logger.Info("Starting Kafka consumer...")
		if err := consumer.Start(); err != nil {
			logger.Errorw("Kafka consumer error", "error", err)
		}
	}()

	// Wait for interrupt signal to gracefully shutdown
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logger.Info("Shutting down services...")

	// Give outstanding requests and consumer 30 seconds to complete
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	// Shutdown HTTP server
	if err := srv.Shutdown(ctx); err != nil {
		logger.Errorw("HTTP server forced to shutdown", "error", err)
	}

	// Shutdown Kafka consumer
	if err := consumer.Stop(); err != nil {
		logger.Errorw("Error stopping Kafka consumer", "error", err)
	}

	// Wait for goroutines to finish
	wg.Wait()
	logger.Info("All services exited gracefully")
}
