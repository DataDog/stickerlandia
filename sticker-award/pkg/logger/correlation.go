package logger

import (
	"context"
	"strconv"

	"go.uber.org/zap"
	ddtrace "gopkg.in/DataDog/dd-trace-go.v1/ddtrace"
	"gopkg.in/DataDog/dd-trace-go.v1/ddtrace/tracer"
)

// WithTrace returns a *zap.SugaredLogger augmented with Datadog correlation
// fields (dd.trace_id and dd.span_id) extracted from the span stored in ctx.
//
// If ctx does not contain a Datadog span, the original logger is returned
// unchanged so callers can use it unconditionally.
func WithTrace(ctx context.Context, l *zap.SugaredLogger) *zap.SugaredLogger {
	if ctx == nil || l == nil {
		return l
	}

	span, ok := tracer.SpanFromContext(ctx)
	if !ok {
		return l
	}

	sc, ok := span.Context().(ddtrace.SpanContext)
	if !ok {
		return l
	}

	return l.With(
		"dd.trace_id", strconv.FormatUint(sc.TraceID(), 10),
		"dd.span_id", strconv.FormatUint(sc.SpanID(), 10),
	)
}
