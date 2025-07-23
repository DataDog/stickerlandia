package com.datadoghq.stickerlandia.stickeraward;

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

        // Use test configuration to prevent actual Kafka connections
        env.put("mp.messaging.outgoing.stickers-assigned.connector", "smallrye-kafka");
        env.put(
                "mp.messaging.outgoing.stickers-assigned.topic",
                "stickers.stickerAssignedToUser.v1");

        env.put("mp.messaging.outgoing.stickers-removed.connector", "smallrye-kafka");
        env.put(
                "mp.messaging.outgoing.stickers-removed.topic",
                "stickers.stickerRemovedFromUser.v1");

        env.put("mp.messaging.outgoing.stickers-claimed.connector", "smallrye-kafka");
        env.put("mp.messaging.outgoing.stickers-claimed.topic", "users.stickerClaimed.v1");

        return env;
    }

    @Override
    public void stop() {
        // No resources to clean up
    }
}
