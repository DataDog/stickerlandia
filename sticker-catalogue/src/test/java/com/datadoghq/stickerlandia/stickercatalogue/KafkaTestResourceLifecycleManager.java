/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue;

import io.quarkus.test.common.QuarkusTestResourceLifecycleManager;
import java.util.HashMap;
import java.util.Map;

/**
 * Resource lifecycle manager for Kafka in integration tests. This configures properties for testing
 * Kafka-based messaging.
 */
public class KafkaTestResourceLifecycleManager implements QuarkusTestResourceLifecycleManager {

    @Override
    public Map<String, String> start() {
        Map<String, String> env = new HashMap<>();

        // Use test configuration for sticker catalogue events (outgoing)
        env.put("mp.messaging.outgoing.stickers_added.connector", "smallrye-kafka");
        env.put("mp.messaging.outgoing.stickers_added.topic", "stickers.stickerAdded.v1");

        env.put("mp.messaging.outgoing.stickers_updated.connector", "smallrye-kafka");
        env.put("mp.messaging.outgoing.stickers_updated.topic", "stickers.stickerUpdated.v1");

        env.put("mp.messaging.outgoing.stickers_deleted.connector", "smallrye-kafka");
        env.put("mp.messaging.outgoing.stickers_deleted.topic", "stickers.stickerDeleted.v1");

        // Test consumers (incoming) for verification
        env.put("mp.messaging.incoming.stickers_added_test.connector", "smallrye-kafka");
        env.put("mp.messaging.incoming.stickers_added_test.topic", "stickers.stickerAdded.v1");
        env.put("mp.messaging.incoming.stickers_added_test.group.id", "test-group-added");
        env.put("mp.messaging.incoming.stickers_added_test.auto.offset.reset", "earliest");

        env.put("mp.messaging.incoming.stickers_updated_test.connector", "smallrye-kafka");
        env.put("mp.messaging.incoming.stickers_updated_test.topic", "stickers.stickerUpdated.v1");
        env.put("mp.messaging.incoming.stickers_updated_test.group.id", "test-group-updated");
        env.put("mp.messaging.incoming.stickers_updated_test.auto.offset.reset", "earliest");

        env.put("mp.messaging.incoming.stickers_deleted_test.connector", "smallrye-kafka");
        env.put("mp.messaging.incoming.stickers_deleted_test.topic", "stickers.stickerDeleted.v1");
        env.put("mp.messaging.incoming.stickers_deleted_test.group.id", "test-group-deleted");
        env.put("mp.messaging.incoming.stickers_deleted_test.auto.offset.reset", "earliest");

        // Let Quarkus Kafka Dev Services handle the bootstrap servers

        return env;
    }

    @Override
    public void stop() {
        // No resources to clean up
    }
}
