package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/router"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/infrastructure/database"
	"github.com/datadog/stickerlandia/sticker-award/pkg/logger"
	"github.com/lib/pq"
	sqltrace "gopkg.in/DataDog/dd-trace-go.v1/contrib/database/sql"
	"gopkg.in/DataDog/dd-trace-go.v1/ddtrace/tracer"
	"gopkg.in/DataDog/dd-trace-go.v1/profiler"
)

func main() {
	// Initialize configuration
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("Failed to load configuration: %v", err)
	}

	// Register SQL driver with DataDog tracing BEFORE starting tracer
	sqltrace.Register("postgres", &pq.Driver{}, sqltrace.WithServiceName("sticker-award"))

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

	// Initialize HTTP router
	r := router.Setup(db, logger, cfg)

	// Create HTTP server
	srv := &http.Server{
		Addr:           fmt.Sprintf(":%s", cfg.Server.Port),
		Handler:        r,
		ReadTimeout:    15 * time.Second,
		WriteTimeout:   15 * time.Second,
		IdleTimeout:    60 * time.Second,
		MaxHeaderBytes: 1 << 20, // 1MB
	}

	// Start server in a goroutine
	go func() {
		logger.Infow("HTTP server starting", "addr", srv.Addr)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			logger.Fatalw("Failed to start HTTP server", "error", err)
		}
	}()

	// Wait for interrupt signal to gracefully shutdown the server
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logger.Info("Shutting down server...")

	// Give outstanding requests 30 seconds to complete
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	if err := srv.Shutdown(ctx); err != nil {
		logger.Fatalw("Server forced to shutdown", "error", err)
	}

	logger.Info("Server exited")
}
