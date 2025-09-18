// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package models

import (
	"time"
)

// Assignment represents a sticker assignment to a user
type Assignment struct {
	ID         uint       `gorm:"primaryKey;autoIncrement" json:"id"`
	UserID     string     `gorm:"not null;size:255;index:idx_user_sticker" json:"userId"`
	StickerID  string     `gorm:"not null;size:255;index:idx_user_sticker" json:"stickerId"`
	AssignedAt time.Time  `gorm:"not null" json:"assignedAt"`
	RemovedAt  *time.Time `gorm:"index:idx_removed_at" json:"removedAt,omitempty"`
	Reason     *string    `gorm:"size:500" json:"reason,omitempty"`
	CreatedAt  time.Time  `gorm:"not null" json:"createdAt"`
	UpdatedAt  time.Time  `gorm:"not null" json:"updatedAt"`
}

// TableName returns the table name for the Assignment model
func (Assignment) TableName() string {
	return "assignments"
}

// IsActive returns true if the assignment is currently active (not removed)
func (a *Assignment) IsActive() bool {
	return a.RemovedAt == nil
}

// Remove marks the assignment as removed with the current timestamp
func (a *Assignment) Remove() {
	now := time.Now().UTC()
	a.RemovedAt = &now
}
