package com.datadoghq.stickerlandia.stickeraward.award.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;
import java.time.Instant;

/**
 * DTO representing a sticker assignment in the award domain. Contains only assignment information
 * with sticker reference by ID.
 */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"stickerId", "assignedAt", "reason"})
public class StickerAssignmentDTO {

    @JsonProperty("stickerId")
    private String stickerId;

    @JsonProperty("assignedAt")
    private Instant assignedAt;

    @JsonProperty("reason")
    private String reason;

    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    @JsonProperty("stickerId")
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    @JsonProperty("assignedAt")
    public Instant getAssignedAt() {
        return assignedAt;
    }

    @JsonProperty("assignedAt")
    public void setAssignedAt(Instant assignedAt) {
        this.assignedAt = assignedAt;
    }

    @JsonProperty("reason")
    public String getReason() {
        return reason;
    }

    @JsonProperty("reason")
    public void setReason(String reason) {
        this.reason = reason;
    }
}
