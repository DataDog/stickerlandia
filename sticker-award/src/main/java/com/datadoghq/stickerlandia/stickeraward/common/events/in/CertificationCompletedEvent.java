package com.datadoghq.stickerlandia.stickeraward.common.events.in;

import com.datadoghq.stickerlandia.stickeraward.common.events.DomainEvent;
import java.time.Instant;
import java.util.Map;

/**
 * CloudEvent received when a certification is completed. Subscribed from the
 * 'certifications.certificationCompleted.v1' topic.
 */
public class CertificationCompletedEvent extends DomainEvent {

    private static final String EVENT_TYPE =
            "com.datadoghq.stickerlandia.certification.completed.v1";
    private static final String EVENT_SOURCE = "/certification-service";

    /** Default constructor for serialization frameworks. */
    public CertificationCompletedEvent() {
        super(EVENT_TYPE, EVENT_SOURCE);
    }

    /**
     * Get the account ID from the event data.
     *
     * @return The account ID
     */
    public String getAccountId() {
        Map<String, Object> data = (Map<String, Object>) getData();
        return data != null ? (String) data.get("accountId") : null;
    }

    /**
     * Get the certification ID from the event data.
     *
     * @return The certification ID
     */
    public String getCertificationId() {
        Map<String, Object> data = (Map<String, Object>) getData();
        return data != null ? (String) data.get("certificationId") : null;
    }

    /**
     * Get the completion timestamp from the event data.
     *
     * @return The completion timestamp
     */
    public Instant getCompletedAt() {
        Map<String, Object> data = (Map<String, Object>) getData();
        if (data != null && data.get("completedAt") != null) {
            Object completedAt = data.get("completedAt");
            if (completedAt instanceof String) {
                return Instant.parse((String) completedAt);
            } else if (completedAt instanceof Instant) {
                return (Instant) completedAt;
            }
        }
        return null;
    }
}
