package com.datadoghq.stickerlandia.stickercatalogue.dto;

import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyDescription;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;
import java.time.Instant;

/** DTO representing a sticker with all its details. */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickerId",
    "stickerName",
    "stickerDescription",
    "stickerQuantityRemaining",
    "imagePath",
    "createdAt",
    "updatedAt"
})
public class StickerDTO {

    @JsonProperty("stickerId")
    private String stickerId;

    @JsonProperty("stickerName")
    private String stickerName;

    @JsonProperty("stickerDescription")
    private String stickerDescription;

    /** Quantity remaining (-1 for infinite). */
    @JsonProperty("stickerQuantityRemaining")
    @JsonPropertyDescription("Quantity remaining (-1 for infinite)")
    private Integer stickerQuantityRemaining;

    /** Path to the sticker image resource. */
    @JsonProperty("imagePath")
    @JsonPropertyDescription("Path to the sticker image resource")
    private String imagePath;

    @JsonProperty("createdAt")
    private Instant createdAt;

    @JsonProperty("updatedAt")
    private Instant updatedAt;

    @JsonIgnore private String imageKey;

    /** The unique identifier for the sticker. */
    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    /** The unique identifier for the sticker. */
    @JsonProperty("stickerId")
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    /** The name of the sticker. */
    @JsonProperty("stickerName")
    public String getStickerName() {
        return stickerName;
    }

    /** The name of the sticker. */
    @JsonProperty("stickerName")
    public void setStickerName(String stickerName) {
        this.stickerName = stickerName;
    }

    /** The description of the sticker. */
    @JsonProperty("stickerDescription")
    public String getStickerDescription() {
        return stickerDescription;
    }

    /** The description of the sticker. */
    @JsonProperty("stickerDescription")
    public void setStickerDescription(String stickerDescription) {
        this.stickerDescription = stickerDescription;
    }

    /** The quantity remaining for the sticker. */
    @JsonProperty("stickerQuantityRemaining")
    public Integer getStickerQuantityRemaining() {
        return stickerQuantityRemaining;
    }

    /** The quantity remaining for the sticker. */
    @JsonProperty("stickerQuantityRemaining")
    public void setStickerQuantityRemaining(Integer stickerQuantityRemaining) {
        this.stickerQuantityRemaining = stickerQuantityRemaining;
    }

    /** Path to the sticker image resource. */
    @JsonProperty("imagePath")
    public String getImagePath() {
        return imagePath;
    }

    /** Path to the sticker image resource. */
    @JsonProperty("imagePath")
    public void setImagePath(String imagePath) {
        this.imagePath = imagePath;
    }

    @JsonProperty("createdAt")
    public Instant getCreatedAt() {
        return createdAt;
    }

    @JsonProperty("createdAt")
    public void setCreatedAt(Instant createdAt) {
        this.createdAt = createdAt;
    }

    @JsonProperty("updatedAt")
    public Instant getUpdatedAt() {
        return updatedAt;
    }

    @JsonProperty("updatedAt")
    public void setUpdatedAt(Instant updatedAt) {
        this.updatedAt = updatedAt;
    }

    public String getImageKey() {
        return imageKey;
    }

    public void setImageKey(String imageKey) {
        this.imageKey = imageKey;
    }
}
