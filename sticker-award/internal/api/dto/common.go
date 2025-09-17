// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

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
