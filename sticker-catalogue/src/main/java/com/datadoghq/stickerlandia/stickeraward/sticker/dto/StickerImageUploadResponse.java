package com.datadoghq.stickerlandia.stickeraward.sticker.dto;

import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;
import java.time.Instant;

/** Response DTO for sticker image upload operations. */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"stickerId", "imageUrl", "uploadedAt"})
public class StickerImageUploadResponse {

    @JsonProperty("stickerId")
    private String stickerId;

    @JsonProperty("imageUrl")
    private String imageUrl;

    @JsonFormat(
            shape = JsonFormat.Shape.STRING,
            pattern = "yyyy-MM-dd'T'HH:mm:ss'Z'",
            timezone = "UTC")
    @JsonProperty("uploadedAt")
    private Instant uploadedAt;

    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    @JsonProperty("stickerId")
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    @JsonProperty("imageUrl")
    public String getImageUrl() {
        return imageUrl;
    }

    @JsonProperty("imageUrl")
    public void setImageUrl(String imageUrl) {
        this.imageUrl = imageUrl;
    }

    @JsonProperty("uploadedAt")
    public Instant getUploadedAt() {
        return uploadedAt;
    }

    @JsonProperty("uploadedAt")
    public void setUploadedAt(Instant uploadedAt) {
        this.uploadedAt = uploadedAt;
    }
}
