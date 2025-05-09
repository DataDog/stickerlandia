package com.datadoghq.stickerlandia.stickeraward.messaging;

import io.quarkus.kafka.client.serialization.ObjectMapperDeserializer;
import com.datadoghq.stickerlandia.stickeraward.events.CertificationCompletedEvent;

/**
 * Deserializer for the CertificationCompletedEvent from Kafka.
 */
public class CertificationCompletedEventDeserializer extends ObjectMapperDeserializer<CertificationCompletedEvent> {
    
    public CertificationCompletedEventDeserializer() {
        super(CertificationCompletedEvent.class);
    }
}