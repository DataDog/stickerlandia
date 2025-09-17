// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package models

import (
	"testing"
	"time"
)

func TestAssignment_IsActive(t *testing.T) {
	tests := []struct {
		name       string
		assignment Assignment
		want       bool
	}{
		{
			name: "active assignment",
			assignment: Assignment{
				ID:        1,
				UserID:    "user1",
				StickerID: "sticker1",
				RemovedAt: nil,
			},
			want: true,
		},
		{
			name: "removed assignment",
			assignment: Assignment{
				ID:        1,
				UserID:    "user1",
				StickerID: "sticker1",
				RemovedAt: &time.Time{},
			},
			want: false,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			if got := tt.assignment.IsActive(); got != tt.want {
				t.Errorf("Assignment.IsActive() = %v, want %v", got, tt.want)
			}
		})
	}
}

func TestAssignment_Remove(t *testing.T) {
	assignment := Assignment{
		ID:        1,
		UserID:    "user1",
		StickerID: "sticker1",
		RemovedAt: nil,
	}

	// Should be active initially
	if !assignment.IsActive() {
		t.Error("Assignment should be active initially")
	}

	// Remove the assignment
	assignment.Remove()

	// Should no longer be active
	if assignment.IsActive() {
		t.Error("Assignment should not be active after removal")
	}

	// RemovedAt should be set
	if assignment.RemovedAt == nil {
		t.Error("RemovedAt should be set after removal")
	}
}
