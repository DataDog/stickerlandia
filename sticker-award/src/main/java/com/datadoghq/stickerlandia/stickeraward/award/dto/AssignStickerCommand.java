
package com.datadoghq.stickerlandia.stickeraward.award.dto;

import javax.annotation.processing.Generated;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickerId",
    "reason"
})
@Generated("jsonschema2pojo")
public class AssignStickerCommand {

    /**
     * 
     * (Required)
     * 
     */
    @JsonProperty("stickerId")
    private String stickerId;
    @JsonProperty("reason")
    private String reason;

    /**
     * 
     * (Required)
     * 
     */
    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    /**
     * 
     * (Required)
     * 
     */
    @JsonProperty("stickerId")
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
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
