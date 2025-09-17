// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package consumed

// UserRegisteredEvent represents the user registered event from user-management service
type UserRegisteredEvent struct {
	EventName    string `json:"eventName"`
	EventVersion string `json:"eventVersion"`
	AccountID    string `json:"accountId"`
}
