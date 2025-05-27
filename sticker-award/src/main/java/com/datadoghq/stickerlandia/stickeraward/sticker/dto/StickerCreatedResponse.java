package com.datadoghq.stickerlandia.stickeraward.sticker.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickerId",
    "stickerName",
    "imageUrl"
})
public class StickerCreatedResponse {

    @JsonProperty("stickerId")
    private String stickerId;
    @JsonProperty("stickerName")
    private String stickerName;
    @JsonProperty("imageUrl")
    private String imageUrl;

    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    @JsonProperty("stickerId")
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    @JsonProperty("stickerName")
    public String getStickerName() {
        return stickerName;
    }

    @JsonProperty("stickerName")
    public void setStickerName(String stickerName) {
        this.stickerName = stickerName;
    }

    @JsonProperty("imageUrl")
    public String getImageUrl() {
        return imageUrl;
    }

    @JsonProperty("imageUrl")
    public void setImageUrl(String imageUrl) {
        this.imageUrl = imageUrl;
    }

}