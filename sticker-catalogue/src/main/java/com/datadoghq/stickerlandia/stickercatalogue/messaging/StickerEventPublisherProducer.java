/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue.messaging;

import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.inject.Instance;
import jakarta.enterprise.inject.Produces;
import jakarta.inject.Inject;
import org.eclipse.microprofile.config.inject.ConfigProperty;
import org.jboss.logging.Logger;

/**
 * CDI producer that selects the appropriate StickerEventPublisher implementation
 * based on the MESSAGING_PROVIDER configuration property.
 *
 * Uses Instance for lazy lookup with @LookupIfProperty annotations on implementations.
 * Only the implementation matching the current MESSAGING_PROVIDER will be instantiated.
 *
 * Supported providers:
 * - "kafka" (default): Uses SmallRye Reactive Messaging with Kafka
 * - "aws": Uses AWS EventBridge
 */
@ApplicationScoped
public class StickerEventPublisherProducer {

    private static final Logger LOG = Logger.getLogger(StickerEventPublisherProducer.class);

    @ConfigProperty(name = "MESSAGING_PROVIDER", defaultValue = "kafka")
    String messagingProvider;

    @Inject
    Instance<KafkaStickerEventPublisher> kafkaPublisher;

    @Inject
    Instance<EventBridgeStickerEventPublisher> eventBridgePublisher;

    @Produces
    @ApplicationScoped
    public StickerEventPublisher produce() {
        LOG.infof("Selecting messaging provider: %s", messagingProvider);

        return switch (messagingProvider.toLowerCase()) {
            case "aws" -> {
                if (eventBridgePublisher.isUnsatisfied()) {
                    throw new IllegalStateException(
                            "EventBridge publisher not available - check MESSAGING_PROVIDER configuration");
                }
                LOG.info("Using EventBridge event publisher");
                yield eventBridgePublisher.get();
            }
            case "kafka" -> {
                if (kafkaPublisher.isUnsatisfied()) {
                    throw new IllegalStateException(
                            "Kafka publisher not available - check MESSAGING_PROVIDER configuration");
                }
                LOG.info("Using Kafka event publisher");
                yield kafkaPublisher.get();
            }
            default -> throw new IllegalArgumentException(
                    "Unsupported MESSAGING_PROVIDER: " + messagingProvider
                            + " (supported: kafka, aws)");
        };
    }
}
