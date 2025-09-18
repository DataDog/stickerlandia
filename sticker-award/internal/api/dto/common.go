// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package dto

// ProblemDetails represents RFC 7807 Problem Details for HTTP APIs
type ProblemDetails struct {
	Type     *string `json:"type,omitempty"`
	Title    *string `json:"title,omitempty"`
	Status   *int    `json:"status,omitempty"`
	Detail   *string `json:"detail,omitempty"`
	Instance *string `json:"instance,omitempty"`
}

// HealthResponse represents the health check response
type HealthResponse struct {
	Status string `json:"status"`
	Time   string `json:"time"`
}
