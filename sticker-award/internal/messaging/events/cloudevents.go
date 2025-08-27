package events

import (
	"context"
	"fmt"
	"time"

	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/google/uuid"
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
