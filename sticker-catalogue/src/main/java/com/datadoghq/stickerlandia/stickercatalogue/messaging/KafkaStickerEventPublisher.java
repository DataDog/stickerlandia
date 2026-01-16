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
import io.opentelemetry.api.trace.Span;
import io.opentelemetry.api.trace.SpanContext;
import io.quarkus.arc.lookup.LookupIfProperty;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.inject.Typed;
import jakarta.inject.Inject;
import java.util.concurrent.CompletionStage;
import org.eclipse.microprofile.reactive.messaging.Channel;
import org.eclipse.microprofile.reactive.messaging.Emitter;
import org.jboss.logging.Logger;

/**
 * Kafka implementation of StickerEventPublisher using SmallRye Reactive Messaging. Publishes
 * sticker catalogue events to Kafka channels following CloudEvents specification. Only activated
 * when MESSAGING_PROVIDER=kafka.
 */
@ApplicationScoped
@Typed(KafkaStickerEventPublisher.class)
@LookupIfProperty(name = "MESSAGING_PROVIDER", stringValue = "kafka")
public class KafkaStickerEventPublisher implements StickerEventPublisher {

    private static final Logger LOG = Logger.getLogger(KafkaStickerEventPublisher.class);
    private static final String SOURCE = "sticker-catalogue";

    @Inject
    @Channel("stickers_added")
    Emitter<CloudEvent<StickerAddedEvent>> stickerAddedEmitter;

    @Inject
    @Channel("stickers_updated")
    Emitter<CloudEvent<StickerUpdatedEvent>> stickerUpdatedEmitter;

    @Inject
    @Channel("stickers_deleted")
    Emitter<CloudEvent<StickerDeletedEvent>> stickerDeletedEmitter;

    @Override
    public CompletionStage<Void> publishStickerAdded(
            String stickerId, String name, String description) {
        StickerAddedEvent eventData = new StickerAddedEvent(stickerId, name, description, null);

        CloudEvent<StickerAddedEvent> cloudEvent =
                createCloudEvent(StickerAddedEvent.EVENT_TYPE, eventData);

        LOG.infof("Publishing sticker added event to Kafka for sticker ID: %s", stickerId);
        return stickerAddedEmitter.send(cloudEvent);
    }

    @Override
    public CompletionStage<Void> publishStickerUpdated(
            String stickerId, String name, String description) {
        StickerUpdatedEvent eventData = new StickerUpdatedEvent(stickerId, name, description, null);

        CloudEvent<StickerUpdatedEvent> cloudEvent =
                createCloudEvent(StickerUpdatedEvent.EVENT_TYPE, eventData);

        LOG.infof("Publishing sticker updated event to Kafka for sticker ID: %s", stickerId);
        return stickerUpdatedEmitter.send(cloudEvent);
    }

    @Override
    public CompletionStage<Void> publishStickerDeleted(String stickerId, String name) {
        StickerDeletedEvent eventData = new StickerDeletedEvent(stickerId, name);

        CloudEvent<StickerDeletedEvent> cloudEvent =
                createCloudEvent(StickerDeletedEvent.EVENT_TYPE, eventData);

        LOG.infof("Publishing sticker deleted event to Kafka for sticker ID: %s", stickerId);
        return stickerDeletedEmitter.send(cloudEvent);
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
