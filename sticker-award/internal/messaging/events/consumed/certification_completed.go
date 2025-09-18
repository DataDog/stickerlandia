// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package consumed

import "time"

// CertificationCompletedEvent represents the event consumed when a certification is completed
type CertificationCompletedEvent struct {
	EventName       string    `json:"eventName"`
	EventVersion    string    `json:"eventVersion"`
	AccountID       string    `json:"accountId"`
	CertificationID string    `json:"certificationId"`
	CompletedAt     time.Time `json:"completedAt"`
}

// NewCertificationCompletedEvent creates a new CertificationCompletedEvent
func NewCertificationCompletedEvent(userID, certificationID string, completedAt time.Time) *CertificationCompletedEvent {
	return &CertificationCompletedEvent{
		EventName:       "CertificationCompleted",
		EventVersion:    "v1",
		AccountID:       userID,
		CertificationID: certificationID,
		CompletedAt:     completedAt,
	}
}
