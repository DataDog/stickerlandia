// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package aws

import (
	"testing"
	"time"

	"github.com/google/uuid"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestGenerateEventID(t *testing.T) {
	// Generate multiple IDs and verify they're valid UUIDs
	ids := make(map[string]bool)

	for i := 0; i < 100; i++ {
		id := generateEventID()

		// Verify it's a valid UUID format
		_, err := uuid.Parse(id)
		require.NoError(t, err, "Generated ID should be valid UUID: %s", id)

		// Verify it's unique
		assert.False(t, ids[id], "Generated IDs should be unique")
		ids[id] = true
	}

	// Verify we got 100 unique IDs
	assert.Equal(t, 100, len(ids))
}

func TestGenerateTimestamp(t *testing.T) {
	timestamp := generateTimestamp()

	// Verify it's not empty
	require.NotEmpty(t, timestamp)

	// Verify it's valid RFC3339 format (CloudEvents requirement)
	parsedTime, err := time.Parse(time.RFC3339, timestamp)
	require.NoError(t, err, "Timestamp should be valid RFC3339 format")

	// Verify it's a recent timestamp (within 1 second)
	now := time.Now()
	diff := now.Sub(parsedTime)
	assert.True(t, diff < 1*time.Second && diff > -1*time.Second,
		"Timestamp should be current time (within 1 second)")
}

func TestEventBridgeCarrier_Set(t *testing.T) {
	carrier := &EventBridgeCarrier{
		headers: make(map[string]string),
	}

	carrier.Set("key1", "value1")
	carrier.Set("key2", "value2")

	assert.Equal(t, "value1", carrier.headers["key1"])
	assert.Equal(t, "value2", carrier.headers["key2"])
	assert.Equal(t, 2, len(carrier.headers))
}

func TestEventBridgeCarrier_Set_Overwrite(t *testing.T) {
	carrier := &EventBridgeCarrier{
		headers: make(map[string]string),
	}

	carrier.Set("key", "value1")
	carrier.Set("key", "value2")

	assert.Equal(t, "value2", carrier.headers["key"])
	assert.Equal(t, 1, len(carrier.headers))
}

func TestEventBridgeCarrier_ForeachKey(t *testing.T) {
	carrier := &EventBridgeCarrier{
		headers: map[string]string{
			"header1": "value1",
			"header2": "value2",
			"header3": "value3",
		},
	}

	collected := make(map[string]string)
	err := carrier.ForeachKey(func(key, val string) error {
		collected[key] = val
		return nil
	})

	require.NoError(t, err)
	assert.Equal(t, carrier.headers, collected)
}

func TestEventBridgeCarrier_ForeachKey_Empty(t *testing.T) {
	carrier := &EventBridgeCarrier{
		headers: make(map[string]string),
	}

	count := 0
	err := carrier.ForeachKey(func(key, val string) error {
		count++
		return nil
	})

	require.NoError(t, err)
	assert.Equal(t, 0, count)
}

func TestEventBridgeCarrier_ForeachKey_ErrorPropagation(t *testing.T) {
	carrier := &EventBridgeCarrier{
		headers: map[string]string{
			"key": "value",
		},
	}

	testErr := assert.AnError
	err := carrier.ForeachKey(func(key, val string) error {
		return testErr
	})

	assert.Equal(t, testErr, err)
}

func TestEventBridgeProducer_Close(t *testing.T) {
	// Create a producer with nil client (we're not testing AWS connection)
	producer := &EventBridgeProducer{
		client:  nil,
		busName: "test-bus",
		source:  "test-source",
	}

	err := producer.Close()

	// Close should always succeed (it's a no-op for EventBridge)
	assert.NoError(t, err)
}
