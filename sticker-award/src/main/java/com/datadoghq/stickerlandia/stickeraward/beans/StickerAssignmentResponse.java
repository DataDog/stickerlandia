
package com.datadoghq.stickerlandia.stickeraward.beans;

import java.util.Date;
import javax.annotation.processing.Generated;
import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "userId",
    "stickerId",
    "assignedAt"
})
@Generated("jsonschema2pojo")
public class StickerAssignmentResponse {

    @JsonProperty("userId")
    private String userId;
    @JsonProperty("stickerId")
    private String stickerId;
    @JsonFormat(shape = JsonFormat.Shape.STRING, pattern = "yyyy-MM-dd'T'HH:mm:ss'Z'", timezone = "UTC")
    @JsonProperty("assignedAt")
    private Date assignedAt;

    @JsonProperty("userId")
    public String getUserId() {
        return userId;
    }

    @JsonProperty("userId")
    public void setUserId(String userId) {
        this.userId = userId;
    }

    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    @JsonProperty("stickerId")
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    @JsonProperty("assignedAt")
    public Date getAssignedAt() {
        return assignedAt;
    }

    @JsonProperty("assignedAt")
    public void setAssignedAt(Date assignedAt) {
        this.assignedAt = assignedAt;
    }

}
