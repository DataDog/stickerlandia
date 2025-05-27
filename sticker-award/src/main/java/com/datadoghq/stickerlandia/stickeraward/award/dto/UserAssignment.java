package com.datadoghq.stickerlandia.stickeraward.award.dto;

import java.util.Date;
import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "userId",
    "assignedAt",
    "reason"
})
public class UserAssignment {

    @JsonProperty("userId")
    private String userId;
    @JsonFormat(shape = JsonFormat.Shape.STRING, pattern = "yyyy-MM-dd'T'HH:mm:ss'Z'", timezone = "UTC")
    @JsonProperty("assignedAt")
    private Date assignedAt;
    @JsonProperty("reason")
    private String reason;

    @JsonProperty("userId")
    public String getUserId() {
        return userId;
    }

    @JsonProperty("userId")
    public void setUserId(String userId) {
        this.userId = userId;
    }

    @JsonProperty("assignedAt")
    public Date getAssignedAt() {
        return assignedAt;
    }

    @JsonProperty("assignedAt")
    public void setAssignedAt(Date assignedAt) {
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