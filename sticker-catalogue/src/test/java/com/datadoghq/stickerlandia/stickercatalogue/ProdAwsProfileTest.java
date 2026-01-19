/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.datadoghq.stickerlandia.stickercatalogue.messaging.EventBridgeStickerEventPublisher;
import com.datadoghq.stickerlandia.stickercatalogue.messaging.StickerEventPublisher;
import io.quarkus.arc.ClientProxy;
import io.quarkus.test.junit.QuarkusTest;
import io.quarkus.test.junit.QuarkusTestProfile;
import io.quarkus.test.junit.TestProfile;
import jakarta.inject.Inject;
import java.util.Map;
import org.junit.jupiter.api.Test;

/**
 * Tests to verify prod-aws profile wires up the correct messaging implementation.
 *
 * <p>In prod-aws mode, the StickerEventPublisher should be the EventBridge implementation, not the
 * Kafka implementation.
 */
@QuarkusTest
@TestProfile(ProdAwsProfileTest.ProdAwsTestProfile.class)
class ProdAwsProfileTest {

    public static class ProdAwsTestProfile implements QuarkusTestProfile {
        @Override
        public String getConfigProfile() {
            return "prod-aws";
        }

        @Override
        public Map<String, String> getConfigOverrides() {
            // Enable DevServices for test infrastructure, provide EventBridge config
            return Map.of(
                    "quarkus.datasource.devservices.enabled", "true",
                    "quarkus.s3.devservices.enabled", "true",
                    "EVENT_BUS_NAME", "test-event-bus");
        }
    }

    @Inject StickerEventPublisher eventPublisher;

    @Test
    void shouldUseEventBridgePublisher() {
        assertNotNull(eventPublisher, "StickerEventPublisher should be injected");

        // Unwrap CDI proxy to get actual implementation
        Object unwrapped = ClientProxy.unwrap(eventPublisher);

        assertTrue(
                unwrapped instanceof EventBridgeStickerEventPublisher,
                "prod-aws profile should wire EventBridgeStickerEventPublisher, but got: "
                        + unwrapped.getClass().getName());
    }
}
