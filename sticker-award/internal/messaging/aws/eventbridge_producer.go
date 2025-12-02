// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package aws

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"github.com/DataDog/dd-trace-go/v2/ddtrace/tracer"
	"github.com/aws/aws-sdk-go-v2/aws"
	awsconfig "github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/service/eventbridge"
	"github.com/aws/aws-sdk-go-v2/service/eventbridge/types"
	"github.com/datadog/stickerlandia/sticker-award/internal/config"
	"github.com/datadog/stickerlandia/sticker-award/internal/messaging/events/published"
	"github.com/google/uuid"
	log "github.com/sirupsen/logrus"
)

// EventBridgeProducer implements the EventPublisher interface for AWS EventBridge
type EventBridgeProducer struct {
	client  *eventbridge.Client
	busName string
	source  string
}

// NewEventBridgeProducer creates a new EventBridge producer with Datadog DSM integration
func NewEventBridgeProducer(cfg *config.AWSConfig) (*EventBridgeProducer, error) {
	// Validate required configuration
	if cfg.EventBusName == "" {
		return nil, fmt.Errorf("EventBridge bus name is required but not configured (EVENT_BUS_NAME)")
	}
	if cfg.Region == "" {
		return nil, fmt.Errorf("AWS region is required but not configured")
	}

	log.WithFields(log.Fields{
		"region":  cfg.Region,
		"busName": cfg.EventBusName,
	}).Info("Creating EventBridge producer")

	// Load AWS configuration
	awsCfg, err := awsconfig.LoadDefaultConfig(context.Background(),
		awsconfig.WithRegion(cfg.Region),
	)
	if err != nil {
		return nil, fmt.Errorf("failed to load AWS config: %w", err)
	}

	client := eventbridge.NewFromConfig(awsCfg)

	producer := &EventBridgeProducer{
		client:  client,
		busName: cfg.EventBusName,
		source:  "sticker-award",
	}

	log.WithFields(log.Fields{
		"region":  cfg.Region,
		"busName": cfg.EventBusName,
	}).Info("EventBridge producer created successfully")

	return producer, nil
}

// PublishStickerAssignedEvent publishes a sticker assignment event to EventBridge
func (p *EventBridgeProducer) PublishStickerAssignedEvent(ctx context.Context, event *published.StickerAssignedToUserEvent) error {
	return p.publishCloudEvent(ctx, "sticker.assigned", "com.datadog.stickerlandia.sticker.assigned.v1", event)
}

// PublishStickerRemovedEvent publishes a sticker removal event to EventBridge
func (p *EventBridgeProducer) PublishStickerRemovedEvent(ctx context.Context, event *published.StickerRemovedFromUserEvent) error {
	return p.publishCloudEvent(ctx, "sticker.removed", "com.datadog.stickerlandia.sticker.removed.v1", event)
}

// PublishStickerClaimedEvent publishes a sticker claimed event to EventBridge
func (p *EventBridgeProducer) PublishStickerClaimedEvent(ctx context.Context, event *published.StickerClaimedEvent) error {
	return p.publishCloudEvent(ctx, "sticker.claimed", "com.datadog.stickerlandia.sticker.claimed.v1", event)
}

// publishCloudEvent publishes a CloudEvent-formatted event to EventBridge with Datadog instrumentation
func (p *EventBridgeProducer) publishCloudEvent(ctx context.Context, detailType, eventType string, eventData interface{}) error {
	// Extract or create trace span
	span, ok := tracer.SpanFromContext(ctx)
	if !ok {
		span = tracer.StartSpan("eventbridge.publish",
			tracer.ServiceName(p.source),
			tracer.ResourceName(detailType),
		)
		defer span.Finish()
		ctx = tracer.ContextWithSpan(ctx, span)
	}

	// Add span tags for EventBridge operation
	span.SetTag("messaging.system", "aws_eventbridge")
	span.SetTag("messaging.destination", detailType)
	span.SetTag("messaging.operation", "publish")
	span.SetTag("aws.eventbridge.bus", p.busName)

	// Create CloudEvent wrapper
	cloudEvent := map[string]interface{}{
		"specversion":     "1.0",
		"type":            eventType,
		"source":          fmt.Sprintf("https://stickerlandia.com/%s", p.source),
		"id":              generateEventID(),
		"time":            generateTimestamp(),
		"datacontenttype": "application/json",
		"data":            eventData,
	}

	// Create DSM carrier for data streams monitoring
	carrier := &EventBridgeCarrier{
		headers: make(map[string]string),
	}

	// Inject trace context and DSM checkpoint into carrier
	if err := tracer.Inject(span.Context(), carrier); err != nil {
		log.WithFields(log.Fields{
			"error": err,
		}).Warn("Failed to inject trace context")
	}

	// Track DSM checkpoint for producer
	TrackDSMProducerCheckpoint(ctx, detailType, carrier)

	// Add carrier headers to CloudEvent if any DSM headers were injected
	if len(carrier.headers) > 0 {
		cloudEvent["datadog"] = carrier.headers
	}

	// Marshal event detail
	detailBytes, err := json.Marshal(cloudEvent)
	if err != nil {
		span.SetTag("error", err)
		return fmt.Errorf("failed to marshal CloudEvent: %w", err)
	}

	// Create EventBridge entry
	entry := types.PutEventsRequestEntry{
		Source:       aws.String(p.source),
		DetailType:   aws.String(detailType),
		Detail:       aws.String(string(detailBytes)),
		EventBusName: aws.String(p.busName),
	}

	// Send to EventBridge
	output, err := p.client.PutEvents(ctx, &eventbridge.PutEventsInput{
		Entries: []types.PutEventsRequestEntry{entry},
	})

	if err != nil {
		span.SetTag("error", err)
		log.WithFields(log.Fields{
			"detailType": detailType,
			"error":      err,
		}).Error("Failed to publish event to EventBridge")
		return fmt.Errorf("failed to publish event: %w", err)
	}

	// Check for partial failures
	if output.FailedEntryCount > 0 {
		for _, entry := range output.Entries {
			if entry.ErrorCode != nil {
				errMsg := fmt.Sprintf("EventBridge error: %s - %s", *entry.ErrorCode, *entry.ErrorMessage)
				span.SetTag("error", errMsg)
				log.WithFields(log.Fields{
					"detailType":   detailType,
					"errorCode":    *entry.ErrorCode,
					"errorMessage": *entry.ErrorMessage,
				}).Error("EventBridge rejected event")
				return fmt.Errorf("%s", errMsg)
			}
		}
	}

	log.WithFields(log.Fields{
		"detailType": detailType,
		"eventType":  eventType,
		"eventId":    output.Entries[0].EventId,
	}).Debug("Event published successfully to EventBridge")

	return nil
}

// Close closes the EventBridge producer
func (p *EventBridgeProducer) Close() error {
	log.Info("EventBridge producer closed")
	return nil
}

// EventBridgeCarrier implements the tracer.TextMapWriter interface for EventBridge messages
type EventBridgeCarrier struct {
	headers map[string]string
}

// Set implements tracer.TextMapWriter
func (c *EventBridgeCarrier) Set(key, val string) {
	c.headers[key] = val
}

// ForeachKey implements tracer.TextMapReader (needed for DSM)
func (c *EventBridgeCarrier) ForeachKey(handler func(key, val string) error) error {
	for k, v := range c.headers {
		if err := handler(k, v); err != nil {
			return err
		}
	}
	return nil
}

// Helper functions

func generateEventID() string {
	// Generate UUID for CloudEvent ID
	return uuid.New().String()
}

func generateTimestamp() string {
	// Use current time in RFC3339 format (CloudEvents requirement)
	return time.Now().Format(time.RFC3339)
}
