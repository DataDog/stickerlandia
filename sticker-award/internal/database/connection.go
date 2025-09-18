// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package database

import (
	"fmt"
	"time"

	gormtrace "github.com/DataDog/dd-trace-go/contrib/gorm.io/gorm.v1/v2"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"
	"gorm.io/gorm/logger"
)

// Connect establishes a connection to the PostgreSQL database
func Connect(cfg *config.DatabaseConfig) (*gorm.DB, error) {
	// Open GORM connection directly with connection string
	db, err := gorm.Open(postgres.Open(cfg.ConnectionString()), &gorm.Config{
		Logger: logger.Default.LogMode(logger.Silent),
		NowFunc: func() time.Time {
			return time.Now().UTC()
		},
	})
	if err != nil {
		return nil, fmt.Errorf("failed to connect to database: %w", err)
	}

	// Register Datadog tracing plugin so that GORM queries emit database spans
	if err := db.Use(gormtrace.NewTracePlugin(gormtrace.WithService("sticker-award-db"))); err != nil {
		return nil, fmt.Errorf("failed to register gorm tracing plugin: %w", err)
	}

	// Get underlying sql.DB to configure connection pool
	underlyingDB, err := db.DB()
	if err != nil {
		return nil, fmt.Errorf("failed to get underlying sql.DB: %w", err)
	}

	// Configure connection pool
	underlyingDB.SetMaxIdleConns(10)                  // Maximum idle connections
	underlyingDB.SetMaxOpenConns(100)                 // Maximum open connections
	underlyingDB.SetConnMaxLifetime(time.Hour)        // Connection max lifetime
	underlyingDB.SetConnMaxIdleTime(10 * time.Minute) // Connection max idle time

	// Test the connection
	if err := underlyingDB.Ping(); err != nil {
		return nil, fmt.Errorf("failed to ping database: %w", err)
	}

	return db, nil
}
