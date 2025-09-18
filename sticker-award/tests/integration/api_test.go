// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

//go:build integration

package integration

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/sirupsen/logrus"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
	"github.com/testcontainers/testcontainers-go"
	"github.com/testcontainers/testcontainers-go/modules/kafka"
	"github.com/testcontainers/testcontainers-go/modules/postgres"
	"github.com/testcontainers/testcontainers-go/wait"
	postgresDriver "gorm.io/driver/postgres"
	"gorm.io/gorm"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/dto"
	"github.com/datadog/stickerlandia/sticker-award/internal/api/router"
	"github.com/datadog/stickerlandia/sticker-award/internal/clients/catalogue"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/database"
	"github.com/datadog/stickerlandia/sticker-award/internal/database/repository"
	"github.com/datadog/stickerlandia/sticker-award/internal/domain/service"
	"github.com/datadog/stickerlandia/sticker-award/pkg/validator"
)

type TestEnvironment struct {
	PostgresContainer *postgres.PostgresContainer
	KafkaContainer    *kafka.KafkaContainer
	WireMockContainer testcontainers.Container
	DB                *gorm.DB
	PostgresURL       string
	KafkaBrokers      []string
	WireMockURL       string
	Router            *gin.Engine
}

func setupTestEnvironment(t *testing.T) *TestEnvironment {
	ctx := context.Background()

	// Start PostgreSQL container
	postgresContainer, err := postgres.Run(ctx,
		"postgres:16",
		postgres.WithDatabase("sticker_awards"),
		postgres.WithUsername("sticker_user"),
		postgres.WithPassword("sticker_password"),
		testcontainers.WithWaitStrategy(
			wait.ForLog("database system is ready to accept connections").
				WithOccurrence(2).
				WithStartupTimeout(30*time.Second)),
	)
	require.NoError(t, err)

	// Get connection details
	postgresURL, err := postgresContainer.ConnectionString(ctx, "sslmode=disable")
	require.NoError(t, err)

	// Connect to database with GORM
	db, err := gorm.Open(postgresDriver.Open(postgresURL), &gorm.Config{})
	require.NoError(t, err)

	// Run migrations
	err = database.RunMigrations(db)
	require.NoError(t, err)

	// Start WireMock container
	wireMockContainer, err := testcontainers.GenericContainer(ctx, testcontainers.GenericContainerRequest{
		ContainerRequest: testcontainers.ContainerRequest{
			Image:        "wiremock/wiremock:3.3.1",
			ExposedPorts: []string{"8080/tcp"},
			WaitingFor:   wait.ForHTTP("/__admin/health").WithPort("8080/tcp"),
		},
		Started: true,
	})
	require.NoError(t, err)

	// Get WireMock URL
	wireMockHost, err := wireMockContainer.Host(ctx)
	require.NoError(t, err)
	wireMockPort, err := wireMockContainer.MappedPort(ctx, "8080")
	require.NoError(t, err)
	wireMockURL := fmt.Sprintf("http://%s:%s", wireMockHost, wireMockPort.Port())

	// Set up test configuration
	cfg := &config.Config{
		Logging: config.LoggingConfig{Level: "debug"},
		Catalogue: config.CatalogueConfig{
			BaseURL: wireMockURL, // Use WireMock URL
			Timeout: 5000,
		},
		Kafka: config.KafkaConfig{
			Brokers:           []string{"localhost:9092"}, // Mock for now
			ProducerTimeout:   5000,
			ProducerRetries:   3,
			ProducerBatchSize: 16384,
			RequireAcks:       1,
			EnableIdempotent:  true,
		},
	}

	// Set up logger
	logger := logrus.New()
	logger.SetLevel(logrus.ErrorLevel) // Reduce noise in tests

	// Set up real dependencies
	assignmentRepo := repository.NewAssignmentRepository(db)
	catalogueClient := catalogue.NewClient(wireMockURL, 5*time.Second)
	validatorInstance := validator.New()
	// Use nil producer for tests to avoid Kafka dependencies
	assignmentService := service.NewAssigner(assignmentRepo, catalogueClient, validatorInstance, nil)

	// Set up router
	gin.SetMode(gin.TestMode)
	router := router.Setup(db, cfg, assignmentService)

	// Clean up function
	t.Cleanup(func() {
		if postgresContainer != nil {
			_ = postgresContainer.Terminate(ctx)
		}
		if wireMockContainer != nil {
			_ = wireMockContainer.Terminate(ctx)
		}
	})

	return &TestEnvironment{
		PostgresContainer: postgresContainer,
		KafkaContainer:    nil, // Not needed for DB migration test
		WireMockContainer: wireMockContainer,
		DB:                db,
		PostgresURL:       postgresURL,
		KafkaBrokers:      nil, // Not needed for DB migration test
		WireMockURL:       wireMockURL,
		Router:            router,
	}
}

func TestDatabaseMigration(t *testing.T) {
	env := setupTestEnvironment(t)

	// Verify that the assignments table exists and has the expected structure
	sqlDB, err := env.DB.DB()
	require.NoError(t, err)

	// Check if assignments table exists
	var tableExists bool
	err = sqlDB.QueryRow(`
		SELECT EXISTS (
			SELECT FROM information_schema.tables 
			WHERE table_schema = 'public' 
			AND table_name = 'assignments'
		)
	`).Scan(&tableExists)
	require.NoError(t, err)
	assert.True(t, tableExists, "assignments table should exist after migration")

	// Verify expected columns exist
	expectedColumns := []string{"id", "user_id", "sticker_id", "assigned_at", "removed_at", "reason", "created_at", "updated_at"}
	for _, col := range expectedColumns {
		var exists bool
		err = sqlDB.QueryRow(`
			SELECT EXISTS (
				SELECT 1 FROM information_schema.columns 
				WHERE table_name = 'assignments' AND column_name = $1
			)
		`, col).Scan(&exists)
		require.NoError(t, err)
		assert.True(t, exists, "Column %s should exist", col)
	}

	// Verify migration version
	version, dirty, err := database.GetMigrationVersion(env.DB)
	require.NoError(t, err)
	assert.False(t, dirty, "Migration should not be in dirty state")
	assert.Greater(t, version, uint(0), "Migration version should be greater than 0")

	// Verify schema_migrations table exists (created by golang-migrate)
	var migrationTableExists bool
	err = sqlDB.QueryRow(`
		SELECT EXISTS (
			SELECT FROM information_schema.tables 
			WHERE table_schema = 'public' 
			AND table_name = 'schema_migrations'
		)
	`).Scan(&migrationTableExists)
	require.NoError(t, err)
	assert.True(t, migrationTableExists, "schema_migrations table should exist")
}

func TestCreateAssignment(t *testing.T) {
	env := setupTestEnvironment(t)

	// Configure WireMock stub for sticker validation
	stickerStub := map[string]interface{}{
		"request": map[string]interface{}{
			"method":  "GET",
			"urlPath": "/api/stickers/v1/test-sticker-1",
		},
		"response": map[string]interface{}{
			"status": 200,
			"jsonBody": map[string]interface{}{
				"stickerId":                "test-sticker-1",
				"stickerName":              "Test Sticker",
				"stickerDescription":       "A test sticker for integration testing",
				"stickerQuantityRemaining": 100,
				"imageUrl":                 "https://example.com/sticker.png",
				"createdAt":                "2023-01-01T00:00:00Z",
				"updatedAt":                "2023-01-01T00:00:00Z",
			},
			"headers": map[string]interface{}{
				"Content-Type": "application/json",
			},
		},
	}

	stubJSON, err := json.Marshal(stickerStub)
	require.NoError(t, err)

	// Configure WireMock stub via HTTP API
	stubReq, err := http.NewRequest(http.MethodPost, env.WireMockURL+"/__admin/mappings", strings.NewReader(string(stubJSON)))
	require.NoError(t, err)
	stubReq.Header.Set("Content-Type", "application/json")

	client := &http.Client{Timeout: 10 * time.Second}
	stubResp, err := client.Do(stubReq)
	require.NoError(t, err)
	defer stubResp.Body.Close()
	require.Equal(t, http.StatusCreated, stubResp.StatusCode, "WireMock stub should be created successfully")

	// Create assignment request
	assignRequest := dto.AssignStickerRequest{
		StickerID: "test-sticker-1",
		Reason:    stringPtr("Test assignment reason"),
	}

	requestBody, err := json.Marshal(assignRequest)
	require.NoError(t, err)

	// Make POST request to assign sticker
	req := httptest.NewRequest(http.MethodPost, "/api/awards/v1/assignments/test-user-1", bytes.NewReader(requestBody))
	req.Header.Set("Content-Type", "application/json")

	w := httptest.NewRecorder()
	env.Router.ServeHTTP(w, req)

	// Assert successful creation (this will fail due to missing sticker-catalogue service)
	assert.Equal(t, http.StatusCreated, w.Code, "Assignment should be created successfully")

	// Parse response
	var response dto.StickerAssignmentResponse
	err = json.Unmarshal(w.Body.Bytes(), &response)
	require.NoError(t, err)

	// Verify response data
	assert.Equal(t, "test-user-1", response.UserID)
	assert.Equal(t, "test-sticker-1", response.StickerID)
	assert.WithinDuration(t, time.Now(), response.AssignedAt, 5*time.Second)

	// Verify the assignment was created in the database
	var count int64
	result := env.DB.Model(&struct {
		ID         int64      `gorm:"primaryKey"`
		UserID     string     `gorm:"column:user_id"`
		StickerID  string     `gorm:"column:sticker_id"`
		AssignedAt time.Time  `gorm:"column:assigned_at"`
		RemovedAt  *time.Time `gorm:"column:removed_at"`
		Reason     *string    `gorm:"column:reason"`
		CreatedAt  time.Time  `gorm:"column:created_at"`
		UpdatedAt  time.Time  `gorm:"column:updated_at"`
	}{}).Table("assignments").Where("user_id = ? AND sticker_id = ?", "test-user-1", "test-sticker-1").Count(&count)
	require.NoError(t, result.Error)

	// Should be 1 because assignment should be created successfully
	assert.Equal(t, int64(1), count, "Assignment should be created in database")

	// Now test deletion
	deleteReq := httptest.NewRequest(http.MethodDelete, "/api/awards/v1/assignments/test-user-1/test-sticker-1", nil)
	deleteW := httptest.NewRecorder()
	env.Router.ServeHTTP(deleteW, deleteReq)

	// Assert successful deletion
	assert.Equal(t, http.StatusOK, deleteW.Code, "Assignment should be deleted successfully")

	// Parse delete response
	var deleteResponse dto.StickerRemovalResponse
	err = json.Unmarshal(deleteW.Body.Bytes(), &deleteResponse)
	require.NoError(t, err)

	// Verify delete response data
	assert.Equal(t, "test-user-1", deleteResponse.UserID)
	assert.Equal(t, "test-sticker-1", deleteResponse.StickerID)
	assert.WithinDuration(t, time.Now(), deleteResponse.RemovedAt, 5*time.Second)

	// Verify assignment is soft-deleted in database (removed_at is set)
	var assignment struct {
		ID         int64      `gorm:"primaryKey"`
		UserID     string     `gorm:"column:user_id"`
		StickerID  string     `gorm:"column:sticker_id"`
		AssignedAt time.Time  `gorm:"column:assigned_at"`
		RemovedAt  *time.Time `gorm:"column:removed_at"`
		Reason     *string    `gorm:"column:reason"`
		CreatedAt  time.Time  `gorm:"column:created_at"`
		UpdatedAt  time.Time  `gorm:"column:updated_at"`
	}
	result = env.DB.Table("assignments").Where("user_id = ? AND sticker_id = ?", "test-user-1", "test-sticker-1").First(&assignment)
	require.NoError(t, result.Error)

	// Verify the assignment is soft-deleted (removed_at is not null)
	assert.NotNil(t, assignment.RemovedAt, "Assignment should be soft-deleted with removed_at timestamp")
	assert.WithinDuration(t, time.Now(), *assignment.RemovedAt, 5*time.Second)
}

func TestCreateAssignmentStickerNotFound(t *testing.T) {
	env := setupTestEnvironment(t)

	// Configure WireMock stub for sticker not found (404 response)
	stickerNotFoundStub := map[string]interface{}{
		"request": map[string]interface{}{
			"method":  "GET",
			"urlPath": "/api/stickers/v1/nonexistent-sticker",
		},
		"response": map[string]interface{}{
			"status": 404,
			"headers": map[string]interface{}{
				"Content-Type": "application/json",
			},
		},
	}

	stubJSON, err := json.Marshal(stickerNotFoundStub)
	require.NoError(t, err)

	// Configure WireMock stub via HTTP API
	stubReq, err := http.NewRequest(http.MethodPost, env.WireMockURL+"/__admin/mappings", strings.NewReader(string(stubJSON)))
	require.NoError(t, err)
	stubReq.Header.Set("Content-Type", "application/json")

	client := &http.Client{Timeout: 10 * time.Second}
	stubResp, err := client.Do(stubReq)
	require.NoError(t, err)
	defer stubResp.Body.Close()
	require.Equal(t, http.StatusCreated, stubResp.StatusCode, "WireMock stub should be created successfully")

	// Create assignment request with nonexistent sticker
	assignRequest := dto.AssignStickerRequest{
		StickerID: "nonexistent-sticker",
		Reason:    stringPtr("Test assignment for nonexistent sticker"),
	}

	requestBody, err := json.Marshal(assignRequest)
	require.NoError(t, err)

	// Make POST request to assign nonexistent sticker
	req := httptest.NewRequest(http.MethodPost, "/api/awards/v1/assignments/test-user-1", bytes.NewReader(requestBody))
	req.Header.Set("Content-Type", "application/json")

	w := httptest.NewRecorder()
	env.Router.ServeHTTP(w, req)

	// Assert error response for sticker not found
	assert.Equal(t, http.StatusUnprocessableEntity, w.Code, "Should return 422 when sticker does not exist")

	// Parse error response
	var errorResponse dto.ProblemDetails
	err = json.Unmarshal(w.Body.Bytes(), &errorResponse)
	require.NoError(t, err)

	// Verify error response structure
	assert.Equal(t, http.StatusUnprocessableEntity, *errorResponse.Status)
	assert.Equal(t, "Unprocessable Entity", *errorResponse.Title)
	assert.Contains(t, *errorResponse.Detail, "sticker not found", "Error message should indicate sticker not found")

	// Verify no assignment was created in the database
	var count int64
	result := env.DB.Model(&struct {
		ID         int64      `gorm:"primaryKey"`
		UserID     string     `gorm:"column:user_id"`
		StickerID  string     `gorm:"column:sticker_id"`
		AssignedAt time.Time  `gorm:"column:assigned_at"`
		RemovedAt  *time.Time `gorm:"column:removed_at"`
		Reason     *string    `gorm:"column:reason"`
		CreatedAt  time.Time  `gorm:"column:created_at"`
		UpdatedAt  time.Time  `gorm:"column:updated_at"`
	}{}).Table("assignments").Where("user_id = ? AND sticker_id = ?", "test-user-1", "nonexistent-sticker").Count(&count)
	require.NoError(t, result.Error)

	// Should be 0 because assignment should not be created for nonexistent sticker
	assert.Equal(t, int64(0), count, "Assignment should not be created for nonexistent sticker")
}

func stringPtr(s string) *string {
	return &s
}
