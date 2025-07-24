package events

import (
	"encoding/json"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestNewStickerAssignedToUserEvent(t *testing.T) {
	accountID := "user-123"
	stickerID := "sticker-456"
	assignedAt := time.Now().UTC()
	reason := "Great job on the project!"

	tests := []struct {
		name       string
		accountID  string
		stickerID  string
		assignedAt time.Time
		reason     *string
	}{
		{
			name:       "event with reason",
			accountID:  accountID,
			stickerID:  stickerID,
			assignedAt: assignedAt,
			reason:     &reason,
		},
		{
			name:       "event without reason",
			accountID:  accountID,
			stickerID:  stickerID,
			assignedAt: assignedAt,
			reason:     nil,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			event := NewStickerAssignedToUserEvent(tt.accountID, tt.stickerID, tt.assignedAt, tt.reason)

			// Test event structure
			assert.Equal(t, "StickerAssignedToUser", event.EventName)
			assert.Equal(t, "v1", event.EventVersion)
			assert.Equal(t, tt.accountID, event.AccountID)
			assert.Equal(t, tt.stickerID, event.StickerID)
			assert.Equal(t, tt.assignedAt, event.AssignedAt)

			if tt.reason != nil {
				require.NotNil(t, event.Reason)
				assert.Equal(t, *tt.reason, *event.Reason)
			} else {
				assert.Nil(t, event.Reason)
			}
		})
	}
}

func TestNewStickerRemovedFromUserEvent(t *testing.T) {
	accountID := "user-123"
	stickerID := "sticker-456"
	removedAt := time.Now().UTC()

	event := NewStickerRemovedFromUserEvent(accountID, stickerID, removedAt)

	// Test event structure
	assert.Equal(t, "StickerRemovedFromUser", event.EventName)
	assert.Equal(t, "v1", event.EventVersion)
	assert.Equal(t, accountID, event.AccountID)
	assert.Equal(t, stickerID, event.StickerID)
	assert.Equal(t, removedAt, event.RemovedAt)
}

func TestNewStickerClaimedEvent(t *testing.T) {
	accountID := "user-123"
	stickerID := "sticker-456"
	claimedAt := time.Now().UTC()

	event := NewStickerClaimedEvent(accountID, stickerID, claimedAt)

	// Test event structure
	assert.Equal(t, "StickerClaimed", event.EventName)
	assert.Equal(t, "v1", event.EventVersion)
	assert.Equal(t, accountID, event.AccountID)
	assert.Equal(t, stickerID, event.StickerID)
	assert.Equal(t, claimedAt, event.ClaimedAt)
}

func TestStickerAssignedToUserEvent_JSONSerialization(t *testing.T) {
	accountID := "user-123"
	stickerID := "sticker-456"
	assignedAt := time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)
	reason := "Test reason"

	event := NewStickerAssignedToUserEvent(accountID, stickerID, assignedAt, &reason)

	// Test JSON marshaling
	jsonData, err := json.Marshal(event)
	require.NoError(t, err)

	expectedJSON := `{
		"eventName": "StickerAssignedToUser",
		"eventVersion": "v1",
		"accountId": "user-123",
		"stickerId": "sticker-456",
		"assignedAt": "2023-01-01T12:00:00Z",
		"reason": "Test reason"
	}`

	// Parse both JSON strings to compare structure (ignore formatting)
	var expected, actual map[string]interface{}
	require.NoError(t, json.Unmarshal([]byte(expectedJSON), &expected))
	require.NoError(t, json.Unmarshal(jsonData, &actual))
	assert.Equal(t, expected, actual)

	// Test JSON unmarshaling
	var unmarshaled StickerAssignedToUserEvent
	err = json.Unmarshal(jsonData, &unmarshaled)
	require.NoError(t, err)

	assert.Equal(t, event.EventName, unmarshaled.EventName)
	assert.Equal(t, event.EventVersion, unmarshaled.EventVersion)
	assert.Equal(t, event.AccountID, unmarshaled.AccountID)
	assert.Equal(t, event.StickerID, unmarshaled.StickerID)
	assert.Equal(t, event.AssignedAt, unmarshaled.AssignedAt)
	require.NotNil(t, unmarshaled.Reason)
	assert.Equal(t, *event.Reason, *unmarshaled.Reason)
}

func TestStickerAssignedToUserEvent_JSONSerialization_NoReason(t *testing.T) {
	accountID := "user-123"
	stickerID := "sticker-456"
	assignedAt := time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)

	event := NewStickerAssignedToUserEvent(accountID, stickerID, assignedAt, nil)

	// Test JSON marshaling
	jsonData, err := json.Marshal(event)
	require.NoError(t, err)

	// Should not contain reason field when nil
	var jsonMap map[string]interface{}
	require.NoError(t, json.Unmarshal(jsonData, &jsonMap))
	_, hasReason := jsonMap["reason"]
	assert.False(t, hasReason, "JSON should not contain reason field when nil")
}

func TestStickerRemovedFromUserEvent_JSONSerialization(t *testing.T) {
	accountID := "user-123"
	stickerID := "sticker-456"
	removedAt := time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)

	event := NewStickerRemovedFromUserEvent(accountID, stickerID, removedAt)

	// Test JSON marshaling
	jsonData, err := json.Marshal(event)
	require.NoError(t, err)

	expectedJSON := `{
		"eventName": "StickerRemovedFromUser",
		"eventVersion": "v1",
		"accountId": "user-123",
		"stickerId": "sticker-456",
		"removedAt": "2023-01-01T12:00:00Z"
	}`

	// Parse both JSON strings to compare structure
	var expected, actual map[string]interface{}
	require.NoError(t, json.Unmarshal([]byte(expectedJSON), &expected))
	require.NoError(t, json.Unmarshal(jsonData, &actual))
	assert.Equal(t, expected, actual)
}

func TestStickerClaimedEvent_JSONSerialization(t *testing.T) {
	accountID := "user-123"
	stickerID := "sticker-456"
	claimedAt := time.Date(2023, 1, 1, 12, 0, 0, 0, time.UTC)

	event := NewStickerClaimedEvent(accountID, stickerID, claimedAt)

	// Test JSON marshaling
	jsonData, err := json.Marshal(event)
	require.NoError(t, err)

	expectedJSON := `{
		"eventName": "StickerClaimed",
		"eventVersion": "v1",
		"accountId": "user-123",
		"stickerId": "sticker-456",
		"claimedAt": "2023-01-01T12:00:00Z"
	}`

	// Parse both JSON strings to compare structure
	var expected, actual map[string]interface{}
	require.NoError(t, json.Unmarshal([]byte(expectedJSON), &expected))
	require.NoError(t, json.Unmarshal(jsonData, &actual))
	assert.Equal(t, expected, actual)
}

func TestDomainEvent_Structure(t *testing.T) {
	// Test the base DomainEvent struct can be created and used
	event := DomainEvent{
		EventName:    "TestEvent",
		EventVersion: "v1",
	}

	assert.Equal(t, "TestEvent", event.EventName)
	assert.Equal(t, "v1", event.EventVersion)
}

func TestEventVersionConsistency(t *testing.T) {
	// Ensure all events use the same version format
	assignedEvent := NewStickerAssignedToUserEvent("user", "sticker", time.Now(), nil)
	removedEvent := NewStickerRemovedFromUserEvent("user", "sticker", time.Now())
	claimedEvent := NewStickerClaimedEvent("user", "sticker", time.Now())

	assert.Equal(t, "v1", assignedEvent.EventVersion)
	assert.Equal(t, "v1", removedEvent.EventVersion)
	assert.Equal(t, "v1", claimedEvent.EventVersion)
}
