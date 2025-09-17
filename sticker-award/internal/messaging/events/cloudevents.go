// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package events

import (
	"context"
	"fmt"
	"strconv"
	"strings"
	"time"

	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/google/uuid"
	log "github.com/sirupsen/logrus"
)

// CloudEvent represents a CloudEvents specification v1.0 compliant event
// with W3C trace context support for span links
type CloudEvent[T any] struct {
	Data        T      `json:"data"`
	SpecVersion string `json:"specversion"`
	Type        string `json:"type"`
	Source      string `json:"source"`
	Id          string `json:"id"`
	Time        string `json:"time"`
	TraceParent string `json:"traceparent,omitempty"` // W3C trace context for span links
}

// NewCloudEvent creates a new CloudEvent with W3C trace context from the current span
func NewCloudEvent[T any](ctx context.Context, eventType string, source string, data T) CloudEvent[T] {
	event := CloudEvent[T]{
		SpecVersion: "1.0",
		Type:        eventType,
		Source:      source,
		Id:          uuid.New().String(),
		Time:        time.Now().UTC().Format(time.RFC3339),
		Data:        data,
	}

	// Inject W3C trace context if we have an active span
	if span, ok := tracer.SpanFromContext(ctx); ok {
		spanCtx := span.Context()
		// Create W3C traceparent header format: version-traceId-spanId-flags
		// Using "01" for flags to indicate sampled
		// Ensure proper padding for trace ID (32 hex chars) and span ID (16 hex chars)
		traceID := spanCtx.TraceID()
		spanID := spanCtx.SpanID()

		// Convert uint64 values to proper hex format with padding
		traceParent := fmt.Sprintf("00-%032x-%016x-01", traceID, spanID)
		event.TraceParent = traceParent
	}

	return event
}

// ExtractSpanLinksFromCloudEvent extracts span links from a CloudEvent's traceparent field
func ExtractSpanLinksFromCloudEvent[T any](ctx context.Context, cloudEvent CloudEvent[T]) []tracer.SpanLink {
	var spanLinks []tracer.SpanLink

	if cloudEvent.TraceParent == "" {
		log.WithContext(ctx).Info("No traceparent found in CloudEvent, no span links to create")
		return spanLinks
	}

	// Parse W3C traceparent format: "version-traceId-spanId-flags"
	parts := strings.Split(cloudEvent.TraceParent, "-")
	if len(parts) != 4 {
		log.WithContext(ctx).WithFields(log.Fields{"traceparent": cloudEvent.TraceParent}).Info("Invalid traceparent format - expected 4 parts separated by dashes")
		return spanLinks
	}

	// Convert hex trace ID and span ID to uint64
	// For DD trace go, we need the full 128-bit trace ID but represented as uint64
	// We can take the lower 64 bits of the 128-bit W3C trace ID
	traceIDHex := parts[1]
	if len(traceIDHex) == 32 {
		// Take the lower 64 bits (last 16 hex characters)
		traceIDHex = traceIDHex[16:]
	}
	traceID, err := strconv.ParseUint(traceIDHex, 16, 64)
	if err != nil {
		log.WithContext(ctx).WithFields(log.Fields{
			"traceId":     parts[1],
			"traceIdHex":  traceIDHex,
			"error":       err,
			"traceparent": cloudEvent.TraceParent,
		}).Info("Failed to parse trace ID from traceparent - invalid hex format")
		return spanLinks
	}

	spanID, err := strconv.ParseUint(parts[2], 16, 64)
	if err != nil {
		log.WithContext(ctx).WithFields(log.Fields{
			"spanId":      parts[2],
			"error":       err,
			"traceparent": cloudEvent.TraceParent,
		}).Info("Failed to parse span ID from traceparent - invalid hex format")
		return spanLinks
	}

	// Create span link
	spanLink := tracer.SpanLink{
		TraceID: traceID,
		SpanID:  spanID,
	}

	spanLinks = append(spanLinks, spanLink)
	log.WithContext(ctx).WithFields(log.Fields{
		"trace_id":         traceID,
		"span_id":          spanID,
		"traceparent":      cloudEvent.TraceParent,
		"span_links_count": 1,
	}).Info("Successfully created span link from CloudEvent traceparent")

	return spanLinks
}
