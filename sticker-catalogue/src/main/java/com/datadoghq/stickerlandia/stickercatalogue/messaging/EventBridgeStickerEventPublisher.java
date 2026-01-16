/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue.messaging;

import com.datadoghq.stickerlandia.stickercatalogue.event.CloudEvent;
import com.datadoghq.stickerlandia.stickercatalogue.event.StickerAddedEvent;
import com.datadoghq.stickerlandia.stickercatalogue.event.StickerDeletedEvent;
import com.datadoghq.stickerlandia.stickercatalogue.event.StickerUpdatedEvent;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.opentelemetry.api.trace.Span;
import io.opentelemetry.api.trace.SpanContext;
import io.quarkus.arc.lookup.LookupIfProperty;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.inject.Typed;
import jakarta.inject.Inject;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
import org.eclipse.microprofile.config.inject.ConfigProperty;
import org.jboss.logging.Logger;
import software.amazon.awssdk.services.eventbridge.EventBridgeClient;
import software.amazon.awssdk.services.eventbridge.model.PutEventsRequest;
import software.amazon.awssdk.services.eventbridge.model.PutEventsRequestEntry;
import software.amazon.awssdk.services.eventbridge.model.PutEventsResponse;

/**
 * AWS EventBridge implementation of StickerEventPublisher. Publishes sticker catalogue events to
 * EventBridge following CloudEvents specification. Only activated when MESSAGING_PROVIDER=aws.
 */
@ApplicationScoped
@Typed(EventBridgeStickerEventPublisher.class)
@LookupIfProperty(name = "MESSAGING_PROVIDER", stringValue = "aws")
public class EventBridgeStickerEventPublisher implements StickerEventPublisher {

    private static final Logger LOG = Logger.getLogger(EventBridgeStickerEventPublisher.class);
    private static final String SOURCE = "sticker-catalogue";

    @Inject EventBridgeClient eventBridgeClient;

    @Inject ObjectMapper objectMapper;

    @ConfigProperty(name = "EVENT_BUS_NAME", defaultValue = "default")
    String eventBusName;

    @Override
    public CompletionStage<Void> publishStickerAdded(
            String stickerId, String name, String description) {
        StickerAddedEvent eventData = new StickerAddedEvent(stickerId, name, description, null);
        CloudEvent<StickerAddedEvent> cloudEvent =
                createCloudEvent(StickerAddedEvent.EVENT_TYPE, eventData);

        LOG.infof("Publishing sticker added event to EventBridge for sticker ID: %s", stickerId);
        return publishEvent("stickers.stickerAdded", cloudEvent);
    }

    @Override
    public CompletionStage<Void> publishStickerUpdated(
            String stickerId, String name, String description) {
        StickerUpdatedEvent eventData = new StickerUpdatedEvent(stickerId, name, description, null);
        CloudEvent<StickerUpdatedEvent> cloudEvent =
                createCloudEvent(StickerUpdatedEvent.EVENT_TYPE, eventData);

        LOG.infof("Publishing sticker updated event to EventBridge for sticker ID: %s", stickerId);
        return publishEvent("stickers.stickerUpdated", cloudEvent);
    }

    @Override
    public CompletionStage<Void> publishStickerDeleted(String stickerId, String name) {
        StickerDeletedEvent eventData = new StickerDeletedEvent(stickerId, name);
        CloudEvent<StickerDeletedEvent> cloudEvent =
                createCloudEvent(StickerDeletedEvent.EVENT_TYPE, eventData);

        LOG.infof("Publishing sticker deleted event to EventBridge for sticker ID: %s", stickerId);
        return publishEvent("stickers.stickerDeleted", cloudEvent);
    }

    /**
     * Publishes a CloudEvent to EventBridge.
     *
     * @param detailType the EventBridge detail-type for routing
     * @param cloudEvent the CloudEvent to publish
     * @return completion stage for async processing
     */
    private <T> CompletionStage<Void> publishEvent(String detailType, CloudEvent<T> cloudEvent) {
        return CompletableFuture.runAsync(
                () -> {
                    try {
                        String detail = objectMapper.writeValueAsString(cloudEvent);

                        PutEventsRequestEntry entry =
                                PutEventsRequestEntry.builder()
                                        .source(SOURCE)
                                        .detailType(detailType)
                                        .detail(detail)
                                        .eventBusName(eventBusName)
                                        .build();

                        PutEventsRequest request =
                                PutEventsRequest.builder().entries(entry).build();

                        PutEventsResponse response = eventBridgeClient.putEvents(request);

                        if (response.failedEntryCount() > 0) {
                            response.entries().stream()
                                    .filter(e -> e.errorCode() != null)
                                    .forEach(
                                            e ->
                                                    LOG.errorf(
                                                            "EventBridge error: %s - %s",
                                                            e.errorCode(), e.errorMessage()));
                            throw new RuntimeException("Failed to publish event to EventBridge");
                        }

                        LOG.debugf(
                                "Event published successfully to EventBridge: %s, eventId: %s",
                                detailType, response.entries().get(0).eventId());

                    } catch (JsonProcessingException e) {
                        LOG.errorf("Failed to serialize CloudEvent: %s", e.getMessage());
                        throw new RuntimeException("Failed to serialize CloudEvent", e);
                    }
                });
    }

    /**
     * Creates a CloudEvent with trace context extracted from the current OpenTelemetry span.
     *
     * @param eventType the event type for the CloudEvent
     * @param data the event data payload
     * @return CloudEvent with populated trace context
     */
    private <T> CloudEvent<T> createCloudEvent(String eventType, T data) {
        CloudEvent<T> cloudEvent = new CloudEvent<>(eventType, SOURCE, data);

        // Extract trace context from current OpenTelemetry span
        Span currentSpan = Span.current();
        if (currentSpan != null) {
            SpanContext spanContext = currentSpan.getSpanContext();
            if (spanContext.isValid()) {
                // Create W3C traceparent header: version-traceId-spanId-flags
                String traceparent =
                        String.format(
                                "00-%s-%s-01", spanContext.getTraceId(), spanContext.getSpanId());
                cloudEvent.setTraceParent(traceparent);

                LOG.debugf("Set traceparent for CloudEvent: %s", traceparent);
            }
        }

        return cloudEvent;
    }
}
