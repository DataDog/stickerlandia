// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package aws

import (
	"context"
	"encoding/json"

	"github.com/DataDog/dd-trace-go/v2/datastreams/options"
	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
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

// NewMessagingHandler creates a new messaging middleware wrapper for AWS
func NewMessagingHandler[T any](handler messaging.CloudEventMessageHandler[T], operationName, serviceName string) *MessagingHandler[T] {
	return &MessagingHandler[T]{
		handler:       handler,
		operationName: operationName,
		serviceName:   serviceName,
	}
}

// Handle implements MessageHandler interface and provides complete messaging processing
func (m *MessagingHandler[T]) Handle(ctx context.Context, message *SQSMessage) error {
	log.WithContext(ctx).WithFields(log.Fields{
		"topic": message.Topic,
	}).Info("Processing SQS message")

	// 1. CloudEvent Processing - Parse CloudEvent first to extract span links
	var cloudEvent events.CloudEvent[T]
	var spanLinks []tracer.SpanLink
	parseErr := json.Unmarshal([]byte(message.Body), &cloudEvent)

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
			"rawMessage": message.Body,
			"topic":      message.Topic,
		}).Warn("Failed to parse CloudEvent, creating root span without links")
	}

	// 2. DSM Processing - Track DSM checkpoint using message attributes
	carrier := &SQSCarrier{headers: message.Attributes}
	ctx = TrackDSMConsumerCheckpoint(ctx, message.Topic, carrier)

	// 3. Trace Creation - Create root span with span links (if available)
	spanOpts := []tracer.StartSpanOption{
		tracer.ServiceName(m.serviceName),
		tracer.ResourceName(message.Topic),
		tracer.Tag("messaging.operation.name", "process"),
		tracer.Tag("messaging.operation.type", "process"),
		tracer.Tag("messaging.system", "aws_sqs"),
		tracer.Tag("messaging.message.envelope.size", len(message.Body)),
		tracer.Tag("messaging.destination", message.Topic),
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
		return parseErr
	}

	// 5. Business Logic - Call handler with parsed CloudEvent and new trace context
	// Business handlers are transport-agnostic and don't receive the raw message
	return m.handler.Handle(ctx, cloudEvent)
}

// Topic returns the topic this handler processes
func (m *MessagingHandler[T]) Topic() string {
	return m.handler.Topic()
}

// TrackDSMProducerCheckpoint records a Data Streams Monitoring checkpoint for AWS EventBridge producers
func TrackDSMProducerCheckpoint(ctx context.Context, topic string, carrier *EventBridgeCarrier) {
	ctx, ok := tracer.SetDataStreamsCheckpointWithParams(ctx, options.CheckpointParams{},
		"direction:out", "type:eventbridge", "topic:"+topic)
	if !ok {
		log.WithContext(ctx).Debug("DSM pathway not available for producer")
		return
	}

	log.WithContext(ctx).WithFields(log.Fields{
		"topic": topic,
		"type":  "eventbridge",
	}).Debug("DSM producer checkpoint recorded for EventBridge")
}

// TrackDSMConsumerCheckpoint records a Data Streams Monitoring checkpoint for SQS consumers
func TrackDSMConsumerCheckpoint(ctx context.Context, queueName string, carrier *SQSCarrier) context.Context {
	// Set checkpoint for consumer
	ctx, ok := tracer.SetDataStreamsCheckpointWithParams(ctx, options.CheckpointParams{},
		"direction:in", "type:sqs", "topic:"+queueName)
	if !ok {
		log.WithContext(ctx).Debug("DSM pathway not available for consumer")
		return ctx
	}

	log.WithContext(ctx).WithFields(log.Fields{
		"queueName": queueName,
		"type":      "sqs",
	}).Debug("DSM consumer checkpoint recorded for SQS")

	return ctx
}

// SQSCarrier implements the tracer.TextMapReader and tracer.TextMapWriter interfaces for SQS messages
type SQSCarrier struct {
	headers map[string]string
}

// Set implements tracer.TextMapWriter
func (c *SQSCarrier) Set(key, val string) {
	c.headers[key] = val
}

// ForeachKey implements tracer.TextMapReader
func (c *SQSCarrier) ForeachKey(handler func(key, val string) error) error {
	for k, v := range c.headers {
		if err := handler(k, v); err != nil {
			return err
		}
	}
	return nil
}

// NewSQSCarrier creates a carrier from SQS message attributes
func NewSQSCarrier(messageAttributes map[string]interface{}) *SQSCarrier {
	headers := make(map[string]string)

	// Extract string attributes from SQS message
	for key, value := range messageAttributes {
		if strValue, ok := value.(string); ok {
			headers[key] = strValue
		}
	}

	return &SQSCarrier{headers: headers}
}

// ParseCloudEventFromEventBridge extracts CloudEvent data from an EventBridge message
func ParseCloudEventFromEventBridge(detail string) (map[string]interface{}, error) {
	var cloudEvent map[string]interface{}
	if err := json.Unmarshal([]byte(detail), &cloudEvent); err != nil {
		return nil, err
	}
	return cloudEvent, nil
}

// ExtractDatadogHeadersFromCloudEvent extracts Datadog headers from a CloudEvent
func ExtractDatadogHeadersFromCloudEvent(cloudEvent map[string]interface{}) map[string]string {
	headers := make(map[string]string)

	// Check for datadog field in CloudEvent
	if ddHeaders, ok := cloudEvent["datadog"].(map[string]interface{}); ok {
		for k, v := range ddHeaders {
			if strVal, ok := v.(string); ok {
				headers[k] = strVal
			}
		}
	}

	return headers
}

// ExtractTraceContextFromCarrier extracts trace context from a carrier and returns a span context
func ExtractTraceContextFromCarrier(ctx context.Context, carrier *SQSCarrier) (context.Context, error) {
	spanCtx, err := tracer.Extract(carrier)
	if err != nil {
		return ctx, err
	}

	// Create a new span linked to the extracted context
	span := tracer.StartSpan("sqs.consume",
		tracer.ChildOf(spanCtx),
	)

	return tracer.ContextWithSpan(ctx, span), nil
}
