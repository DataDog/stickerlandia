package logger

import (
	"context"

	"go.uber.org/zap"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
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

	sc := span.Context()
	
	return l.With(
		"dd.trace_id", sc.TraceID(),
		"dd.span_id", sc.SpanID(),
	)
}
