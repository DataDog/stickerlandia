// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package kafka

import (
	"context"
	"encoding/json"
	"fmt"

	ddsarama "github.com/DataDog/dd-trace-go/contrib/IBM/sarama/v2"
	"github.com/DataDog/dd-trace-go/v2/datastreams"
	"github.com/DataDog/dd-trace-go/v2/datastreams/options"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/ext"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/IBM/sarama"
	log "github.com/sirupsen/logrus"

	"github.com/datadog/stickerlandia/sticker-award/internal/messaging"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events"
)

// MessagingHandler wraps a messaging.CloudEventMessageHandler to provide complete messaging infrastructure:
// - DSM tracking
// - Root trace creation with span links
// - CloudEvent parsing
type MessagingHandler[T any] struct {
	handler       messaging.CloudEventMessageHandler[T]
	operationName string
	serviceName   string
}

// NewMessagingHandler creates a new messaging middleware wrapper
func NewMessagingHandler[T any](handler messaging.CloudEventMessageHandler[T], operationName, serviceName string) *MessagingHandler[T] {
	return &MessagingHandler[T]{
		handler:       handler,
		operationName: operationName,
		serviceName:   serviceName,
	}
}

// Handle implements MessageHandler interface and provides complete messaging processing
func (m *MessagingHandler[T]) Handle(ctx context.Context, message *sarama.ConsumerMessage) error {
	log.WithContext(ctx).WithFields(log.Fields{
		"topic":     message.Topic,
		"partition": message.Partition,
		"offset":    message.Offset,
	}).Info("Processing message")

	// 1. DSM Processing - Extract DSM context (but don't inherit trace context)
	dsmCtx := datastreams.ExtractFromBase64Carrier(context.Background(), ddsarama.NewConsumerMessageCarrier(message))
	dsmCtx, _ = tracer.SetDataStreamsCheckpointWithParams(dsmCtx, options.CheckpointParams{
		ServiceOverride: m.serviceName,
	}, "direction:in", "type:kafka", "topic:"+message.Topic, "manual_checkpoint:true")

	// 2. CloudEvent Processing - Try to parse CloudEvent first to extract span links
	var cloudEvent events.CloudEvent[T]
	var spanLinks []tracer.SpanLink
	parseErr := json.Unmarshal(message.Value, &cloudEvent)

	if parseErr == nil {
		// Successfully parsed - extract span links for root span creation
		spanLinks = events.ExtractSpanLinksFromCloudEvent(ctx, cloudEvent)
		if len(spanLinks) > 0 {
			for i, link := range spanLinks {
				log.WithContext(ctx).WithFields(log.Fields{
					"span_link_index": i,
					"trace_id":        link.TraceID,
					"span_id":         link.SpanID,
					"topic":           message.Topic,
				}).Info("Extracted span link from CloudEvent traceparent")
			}
			log.WithContext(ctx).WithFields(log.Fields{
				"span_links_count": len(spanLinks),
				"topic":            message.Topic,
			}).Info("Successfully extracted span links for root span creation")
		} else {
			log.WithContext(ctx).WithFields(log.Fields{
				"topic":       message.Topic,
				"traceparent": cloudEvent.TraceParent,
			}).Warn("No span links extracted - CloudEvent has no valid traceparent")
		}
	} else {
		log.WithContext(ctx).WithFields(log.Fields{
			"error":      parseErr.Error(),
			"rawMessage": string(message.Value),
			"topic":      message.Topic,
		}).Warn("Failed to parse CloudEvent, creating root span without links")
	}

	// 3. Trace Creation - Create root span with span links (if available)
	spanOpts := []tracer.StartSpanOption{
		tracer.SpanType(ext.SpanTypeMessageConsumer),
		tracer.ServiceName(m.serviceName),
		tracer.ResourceName(message.Topic),
		tracer.Tag("messaging.operation.name", "process"),
		tracer.Tag("messaging.operation.type", "process"),
		tracer.Tag("messaging.system", "kafka"),
		tracer.Tag("messaging.message.envelope.size", len(message.Value)),
		// DSM tags
		tracer.Tag("kafka.topic", message.Topic),
		tracer.Tag("kafka.partition", message.Partition),
		tracer.Tag("kafka.offset", message.Offset),
		tracer.Tag("span.kind", "consumer"),
	}

	// Add span links to root span creation if we have them
	if len(spanLinks) > 0 {
		spanOpts = append(spanOpts, tracer.WithSpanLinks(spanLinks))
		log.WithContext(ctx).WithFields(log.Fields{
			"span_links_count": len(spanLinks),
			"topic":            message.Topic,
			"operation":        m.operationName,
		}).Info("Creating root span WITH span links")
	} else {
		log.WithContext(ctx).WithFields(log.Fields{
			"topic":     message.Topic,
			"operation": m.operationName,
		}).Info("Creating root span WITHOUT span links")
	}

	// Start a new ROOT trace for message processing
	span := tracer.StartSpan(m.operationName, spanOpts...)
	ctx = tracer.ContextWithSpan(ctx, span)
	defer span.Finish()

	// 4. Error Handling - If parsing failed, record error and return early
	if parseErr != nil {
		span.SetTag("error", true)
		span.SetTag("error.msg", parseErr.Error())
		log.WithContext(ctx).Error("Failed to parse CloudEvent")
		return fmt.Errorf("failed to parse CloudEvent: %w", parseErr)
	}

	// 5. Business Logic - Call handler with parsed CloudEvent and new trace context
	// Business handlers are transport-agnostic and don't receive the raw message
	return m.handler.Handle(ctx, cloudEvent)
}

// Topic returns the topic this handler processes
func (m *MessagingHandler[T]) Topic() string {
	return m.handler.Topic()
}
