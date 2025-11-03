// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package aws

import (
	"context"
	"encoding/json"
	"fmt"
	"testing"

	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestSQSCarrier_Set(t *testing.T) {
	carrier := &SQSCarrier{
		headers: make(map[string]string),
	}

	carrier.Set("trace-id", "12345")
	carrier.Set("span-id", "67890")

	assert.Equal(t, "12345", carrier.headers["trace-id"])
	assert.Equal(t, "67890", carrier.headers["span-id"])
}

func TestSQSCarrier_ForeachKey(t *testing.T) {
	carrier := &SQSCarrier{
		headers: map[string]string{
			"x-datadog-trace-id":  "123",
			"x-datadog-parent-id": "456",
			"dd-pathway-ctx":      "encoded_pathway",
		},
	}

	collected := make(map[string]string)
	err := carrier.ForeachKey(func(key, val string) error {
		collected[key] = val
		return nil
	})

	require.NoError(t, err)
	assert.Equal(t, 3, len(collected))
	assert.Equal(t, "123", collected["x-datadog-trace-id"])
	assert.Equal(t, "456", collected["x-datadog-parent-id"])
	assert.Equal(t, "encoded_pathway", collected["dd-pathway-ctx"])
}

func TestSQSCarrier_ForeachKey_Empty(t *testing.T) {
	carrier := &SQSCarrier{
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

func TestNewSQSCarrier(t *testing.T) {
	messageAttributes := map[string]interface{}{
		"trace-id": "12345",
		"span-id":  "67890",
		"count":    42, // Non-string should be ignored
	}

	carrier := NewSQSCarrier(messageAttributes)

	require.NotNil(t, carrier)
	assert.Equal(t, "12345", carrier.headers["trace-id"])
	assert.Equal(t, "67890", carrier.headers["span-id"])
	assert.NotContains(t, carrier.headers, "count") // Non-string ignored
}

func TestNewSQSCarrier_Empty(t *testing.T) {
	carrier := NewSQSCarrier(map[string]interface{}{})

	require.NotNil(t, carrier)
	assert.Equal(t, 0, len(carrier.headers))
}

func TestNewSQSCarrier_OnlyStrings(t *testing.T) {
	messageAttributes := map[string]interface{}{
		"integer": 123,
		"float":   45.67,
		"bool":    true,
		"nil":     nil,
		"string":  "valid",
	}

	carrier := NewSQSCarrier(messageAttributes)

	require.NotNil(t, carrier)
	assert.Equal(t, 1, len(carrier.headers))
	assert.Equal(t, "valid", carrier.headers["string"])
}

func TestParseCloudEventFromEventBridge(t *testing.T) {
	cloudEventJSON := `{
		"specversion": "1.0",
		"type": "com.datadog.stickerlandia.sticker.assigned.v1",
		"source": "https://stickerlandia.com/sticker-award",
		"id": "test-id-123",
		"time": "2025-01-01T12:00:00Z",
		"datacontenttype": "application/json",
		"data": {
			"userId": "user-123",
			"stickerId": "sticker-456"
		}
	}`

	cloudEvent, err := ParseCloudEventFromEventBridge(cloudEventJSON)

	require.NoError(t, err)
	require.NotNil(t, cloudEvent)

	assert.Equal(t, "1.0", cloudEvent["specversion"])
	assert.Equal(t, "com.datadog.stickerlandia.sticker.assigned.v1", cloudEvent["type"])
	assert.Equal(t, "https://stickerlandia.com/sticker-award", cloudEvent["source"])
	assert.Equal(t, "test-id-123", cloudEvent["id"])

	// Verify data is parsed
	data, ok := cloudEvent["data"].(map[string]interface{})
	require.True(t, ok)
	assert.Equal(t, "user-123", data["userId"])
	assert.Equal(t, "sticker-456", data["stickerId"])
}

func TestParseCloudEventFromEventBridge_InvalidJSON(t *testing.T) {
	invalidJSON := `{invalid json`

	cloudEvent, err := ParseCloudEventFromEventBridge(invalidJSON)

	assert.Error(t, err)
	assert.Nil(t, cloudEvent)
}

func TestParseCloudEventFromEventBridge_EmptyString(t *testing.T) {
	cloudEvent, err := ParseCloudEventFromEventBridge("")

	assert.Error(t, err)
	assert.Nil(t, cloudEvent)
}

func TestExtractDatadogHeadersFromCloudEvent(t *testing.T) {
	cloudEvent := map[string]interface{}{
		"specversion": "1.0",
		"type":        "test.event",
		"datadog": map[string]interface{}{
			"x-datadog-trace-id":  "123",
			"x-datadog-parent-id": "456",
			"dd-pathway-ctx":      "encoded",
		},
	}

	headers := ExtractDatadogHeadersFromCloudEvent(cloudEvent)

	require.NotNil(t, headers)
	assert.Equal(t, 3, len(headers))
	assert.Equal(t, "123", headers["x-datadog-trace-id"])
	assert.Equal(t, "456", headers["x-datadog-parent-id"])
	assert.Equal(t, "encoded", headers["dd-pathway-ctx"])
}

func TestExtractDatadogHeadersFromCloudEvent_NoDatadog(t *testing.T) {
	cloudEvent := map[string]interface{}{
		"specversion": "1.0",
		"type":        "test.event",
	}

	headers := ExtractDatadogHeadersFromCloudEvent(cloudEvent)

	require.NotNil(t, headers)
	assert.Equal(t, 0, len(headers))
}

func TestExtractDatadogHeadersFromCloudEvent_EmptyDatadog(t *testing.T) {
	cloudEvent := map[string]interface{}{
		"specversion": "1.0",
		"datadog":     map[string]interface{}{},
	}

	headers := ExtractDatadogHeadersFromCloudEvent(cloudEvent)

	require.NotNil(t, headers)
	assert.Equal(t, 0, len(headers))
}

func TestExtractDatadogHeadersFromCloudEvent_NonStringValues(t *testing.T) {
	cloudEvent := map[string]interface{}{
		"datadog": map[string]interface{}{
			"string-header": "valid",
			"int-header":    123,
			"bool-header":   true,
		},
	}

	headers := ExtractDatadogHeadersFromCloudEvent(cloudEvent)

	require.NotNil(t, headers)
	assert.Equal(t, 1, len(headers))
	assert.Equal(t, "valid", headers["string-header"])
	assert.NotContains(t, headers, "int-header")
	assert.NotContains(t, headers, "bool-header")
}

func TestEventBridgeEnvelope_Structure(t *testing.T) {
	// Test that EventBridgeEnvelope can be marshaled/unmarshaled
	envelope := EventBridgeEnvelope{
		Version:    "0",
		ID:         "test-id",
		DetailType: "sticker.assigned",
		Source:     "sticker-award",
		Time:       "2025-01-01T12:00:00Z",
		Region:     "us-east-1",
		Resources:  []string{"arn:aws:events:us-east-1:123:rule/test"},
		Detail:     `{"test": "data"}`,
		Account:    "123456789",
	}

	// Marshal to JSON
	jsonData, err := json.Marshal(envelope)
	require.NoError(t, err)

	// Unmarshal back
	var decoded EventBridgeEnvelope
	err = json.Unmarshal(jsonData, &decoded)
	require.NoError(t, err)

	// Verify fields
	assert.Equal(t, envelope.Version, decoded.Version)
	assert.Equal(t, envelope.ID, decoded.ID)
	assert.Equal(t, envelope.DetailType, decoded.DetailType)
	assert.Equal(t, envelope.Source, decoded.Source)
	assert.Equal(t, envelope.Detail, decoded.Detail)
}

// Mock handler for testing MessagingHandler
type mockCloudEventHandler struct {
	handleCalled  bool
	receivedEvent events.CloudEvent[map[string]interface{}]
	returnError   error
	topicName     string
}

func (m *mockCloudEventHandler) Handle(ctx context.Context, cloudEvent events.CloudEvent[map[string]interface{}]) error {
	m.handleCalled = true
	m.receivedEvent = cloudEvent
	return m.returnError
}

func (m *mockCloudEventHandler) Topic() string {
	return m.topicName
}

func TestMessagingHandler_Handle_Success(t *testing.T) {
	// Create mock business handler
	mockHandler := &mockCloudEventHandler{
		topicName: "test.topic",
	}

	// Create MessagingHandler wrapper
	wrapper := NewMessagingHandler[map[string]interface{}](mockHandler, "test.operation", "test-service")

	// Create test SQS message with valid CloudEvent
	cloudEventJSON := `{
		"specversion": "1.0",
		"type": "test.event",
		"source": "test-source",
		"id": "test-id",
		"data": {"key": "value"},
		"datadog": {
			"x-datadog-trace-id": "123",
			"dd-pathway-ctx": "encoded"
		}
	}`

	sqsMessage := &SQSMessage{
		Body:       cloudEventJSON,
		Topic:      "test.topic",
		Attributes: map[string]string{},
	}

	// Call handler
	err := wrapper.Handle(context.Background(), sqsMessage)

	// Verify
	require.NoError(t, err)
	assert.True(t, mockHandler.handleCalled)
	assert.NotNil(t, mockHandler.receivedEvent)
}

func TestMessagingHandler_Handle_ParseError(t *testing.T) {
	// Create mock business handler
	mockHandler := &mockCloudEventHandler{
		topicName: "test.topic",
	}

	// Create MessagingHandler wrapper
	wrapper := NewMessagingHandler[map[string]interface{}](mockHandler, "test.operation", "test-service")

	// Create test SQS message with INVALID CloudEvent JSON
	sqsMessage := &SQSMessage{
		Body:       "invalid json{",
		Topic:      "test.topic",
		Attributes: map[string]string{},
	}

	// Call handler
	err := wrapper.Handle(context.Background(), sqsMessage)

	// Verify - should return parse error and NOT call business handler
	require.Error(t, err)
	assert.False(t, mockHandler.handleCalled, "Business handler should not be called on parse error")
}

func TestMessagingHandler_Handle_BusinessLogicError(t *testing.T) {
	// Create mock business handler that returns error
	expectedError := fmt.Errorf("business logic error")
	mockHandler := &mockCloudEventHandler{
		topicName:   "test.topic",
		returnError: expectedError,
	}

	// Create MessagingHandler wrapper
	wrapper := NewMessagingHandler[map[string]interface{}](mockHandler, "test.operation", "test-service")

	// Create test SQS message with valid CloudEvent
	cloudEventJSON := `{
		"specversion": "1.0",
		"type": "test.event",
		"source": "test-source",
		"id": "test-id",
		"data": {"key": "value"}
	}`

	sqsMessage := &SQSMessage{
		Body:       cloudEventJSON,
		Topic:      "test.topic",
		Attributes: map[string]string{},
	}

	// Call handler
	err := wrapper.Handle(context.Background(), sqsMessage)

	// Verify - should propagate business logic error
	require.Error(t, err)
	assert.Equal(t, expectedError, err)
	assert.True(t, mockHandler.handleCalled, "Business handler should be called")
}

func TestMessagingHandler_Topic(t *testing.T) {
	mockHandler := &mockCloudEventHandler{
		topicName: "expected.topic.name",
	}

	wrapper := NewMessagingHandler[map[string]interface{}](mockHandler, "test.operation", "test-service")

	assert.Equal(t, "expected.topic.name", wrapper.Topic())
}

func TestNewMessagingHandler(t *testing.T) {
	mockHandler := &mockCloudEventHandler{
		topicName: "test.topic",
	}

	wrapper := NewMessagingHandler[map[string]interface{}](mockHandler, "test-operation", "test-service")

	require.NotNil(t, wrapper)
	assert.Equal(t, mockHandler, wrapper.handler)
	assert.Equal(t, "test-operation", wrapper.operationName)
	assert.Equal(t, "test-service", wrapper.serviceName)
}
