// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package main

import (
	"context"
	"fmt"

	log "github.com/sirupsen/logrus"

	"net/http"
	"os"
	"os/signal"
	"sync"
	"syscall"
	"time"

	dd_logrus "github.com/DataDog/dd-trace-go/contrib/sirupsen/logrus/v2"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/DataDog/dd-trace-go/v2/profiler"
	"github.com/datadog/stickerlandia/sticker-award/internal/api/router"
	"github.com/datadog/stickerlandia/sticker-award/internal/clients/catalogue"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/database"
	"github.com/datadog/stickerlandia/sticker-award/internal/database/repository"
	"github.com/datadog/stickerlandia/sticker-award/internal/domain/service"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/factory"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/handlers"
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

	// Initialize logger backend with logrus + Datadog
	log.SetFormatter(&log.JSONFormatter{})
	log.AddHook(&dd_logrus.DDContextLogHook{})

	// Parse log level
	level, err := log.ParseLevel(cfg.Logging.Level)
	if err != nil {
		log.Fatalf("Invalid log level %s: %v", cfg.Logging.Level, err)
	}
	log.SetLevel(level)

	log.WithFields(log.Fields{
		"port": cfg.Server.Port,
	}).Info("Starting Sticker Award Service")

	// Initialize database connection
	db, err := database.Connect(&cfg.Database)
	if err != nil {
		log.WithFields(log.Fields{"error": err}).Fatal("Failed to connect to database")
	}

	// Run database migrations
	log.Info("Running database migrations...")
	if err := database.RunMigrations(db); err != nil {
		log.WithFields(log.Fields{"error": err}).Fatal("Failed to run database migrations")
	}
	log.Info("Database migrations completed successfully")

	// Initialize services dependencies
	assignmentRepo := repository.NewAssignmentRepository(db)
	catalogueClient := catalogue.NewClient(cfg.Catalogue.BaseURL, time.Duration(cfg.Catalogue.Timeout)*time.Second)
	validator := validator.New()

	// Initialize Kafka producer
	producer, err := messaging.NewProducer(&cfg.Kafka)
	if err != nil {
		log.WithFields(log.Fields{"error": err}).Fatal("Failed to create Kafka producer")
	}

	assignmentService := service.NewAssigner(assignmentRepo, catalogueClient, validator, producer)

	// Initialize HTTP router
	r := router.Setup(db, cfg, assignmentService)

	// Log Kafka configuration being used
	log.WithFields(log.Fields{
		"brokers": cfg.Kafka.Brokers,
		"groupID": cfg.Kafka.GroupID,
	}).Info("Kafka configuration loaded")

	// Initialize Kafka consumer
	consumer, err := messaging.NewConsumer(&cfg.Kafka)
	if err != nil {
		log.WithFields(log.Fields{"error": err}).Fatal("Failed to create Kafka consumer")
	}

	// Create handler factory for automatic middleware stacking
	handlerFactory := factory.NewHandlerFactory("sticker-award")

	// Register user registered event handler (automatically gets DSM + CloudEvent middleware)
	userRegisteredHandler := handlers.NewUserRegisteredMessageHandler(assignmentService, handlerFactory)
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
		log.WithFields(log.Fields{"addr": srv.Addr}).Info("HTTP server starting")
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.WithFields(log.Fields{"error": err}).Error("HTTP server error")
		}
	}()

	// Start Kafka consumer in a goroutine
	wg.Add(1)
	go func() {
		defer wg.Done()
		log.Info("Starting Kafka consumer...")
		if err := consumer.Start(); err != nil {
			log.WithFields(log.Fields{"error": err}).Error("Kafka consumer error")
		}
	}()

	// Wait for interrupt signal to gracefully shutdown
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	log.Info("Shutting down services...")

	// Give outstanding requests and consumer 30 seconds to complete
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	// Shutdown HTTP server
	if err := srv.Shutdown(ctx); err != nil {
		log.WithFields(log.Fields{"error": err}).Error("HTTP server forced to shutdown")
	}

	// Shutdown Kafka consumer
	if err := consumer.Stop(); err != nil {
		log.WithFields(log.Fields{"error": err}).Error("Error stopping Kafka consumer")
	}

	// Wait for goroutines to finish
	wg.Wait()
	log.Info("All services exited gracefully")
}
