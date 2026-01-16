/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.datadoghq.stickerlandia.stickercatalogue.messaging.KafkaStickerEventPublisher;
import com.datadoghq.stickerlandia.stickercatalogue.messaging.StickerEventPublisher;
import io.quarkus.arc.ClientProxy;
import io.quarkus.test.junit.QuarkusTest;
import io.quarkus.test.junit.QuarkusTestProfile;
import io.quarkus.test.junit.TestProfile;
import jakarta.inject.Inject;
import java.util.Map;
import org.junit.jupiter.api.Test;

/**
 * Tests to verify prod-kafka profile wires up the correct messaging implementation.
 *
 * <p>In prod-kafka mode, the StickerEventPublisher should be the Kafka implementation.
 */
@QuarkusTest
@TestProfile(ProdKafkaProfileTest.ProdKafkaTestProfile.class)
class ProdKafkaProfileTest {

    public static class ProdKafkaTestProfile implements QuarkusTestProfile {
        @Override
        public String getConfigProfile() {
            return "prod-kafka";
        }

        @Override
        public Map<String, String> getConfigOverrides() {
            // Enable DevServices for test infrastructure
            return Map.of(
                    "quarkus.datasource.devservices.enabled", "true",
                    "quarkus.kafka.devservices.enabled", "true",
                    "quarkus.s3.devservices.enabled", "true");
        }
    }

    @Inject StickerEventPublisher eventPublisher;

    @Test
    void shouldUseKafkaPublisher() {
        assertNotNull(eventPublisher, "StickerEventPublisher should be injected");

        // Unwrap CDI proxy to get actual implementation
        Object unwrapped = ClientProxy.unwrap(eventPublisher);

        assertTrue(
                unwrapped instanceof KafkaStickerEventPublisher,
                "prod-kafka profile should wire KafkaStickerEventPublisher, but got: "
                        + unwrapped.getClass().getName());
    }
}
