/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.common.messaging;

import com.datadoghq.stickerlandia.stickercatalogue.event.CloudEvent;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.Map;
import org.apache.kafka.common.serialization.Serializer;

/**
 * Serializes CloudEvents, which wrap up a concrete, topic-specific payload. This is used by our
 * smallrye Kafka infrastructure.
 */
public class CloudEventSerializer implements Serializer<CloudEvent<?>> {

    private final ObjectMapper mapper = new ObjectMapper();

    @Override
    public void configure(Map<String, ?> configs, boolean isKey) {
        // nothing to configure
    }

    @Override
    public byte[] serialize(String topic, CloudEvent<?> data) {
        if (data == null) {
            return null;
        }
        try {
            return mapper.writeValueAsBytes(data);
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize CloudEvent", e);
        }
    }

    @Override
    public void close() {
        // nothing to close
    }
}
