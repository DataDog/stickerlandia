package com.datadoghq.stickerlandia.stickeraward.common.events.in;

import java.time.Instant;

/**
 * Event received when a certification is completed. Subscribed from the
 * 'certifications.certificationCompleted.v1' topic.
 */
public class CertificationCompletedEvent {

    private String accountId;
    private String certificationId;
    private Instant completedAt;

    public String getAccountId() {
        return accountId;
    }

    public void setAccountId(String accountId) {
        this.accountId = accountId;
    }

    public String getCertificationId() {
        return certificationId;
    }

    public void setCertificationId(String certificationId) {
        this.certificationId = certificationId;
    }

    public Instant getCompletedAt() {
        return completedAt;
    }

    public void setCompletedAt(Instant completedAt) {
        this.completedAt = completedAt;
    }
}
