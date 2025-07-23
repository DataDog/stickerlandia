package com.datadoghq.stickerlandia.stickercatalogue.dto;

import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyDescription;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;
import java.util.Date;

/** DTO representing a sticker with all its details. */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickerId",
    "stickerName",
    "stickerDescription",
    "stickerQuantityRemaining",
    "imageUrl",
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

    /** URL to the sticker image resource. */
    @JsonProperty("imageUrl")
    @JsonPropertyDescription("URL to the sticker image resource")
    private String imageUrl;

    @JsonFormat(
            shape = JsonFormat.Shape.STRING,
            pattern = "yyyy-MM-dd'T'HH:mm:ss'Z'",
            timezone = "UTC")
    @JsonProperty("createdAt")
    private Date createdAt;

    @JsonFormat(
            shape = JsonFormat.Shape.STRING,
            pattern = "yyyy-MM-dd'T'HH:mm:ss'Z'",
            timezone = "UTC")
    @JsonProperty("updatedAt")
    private Date updatedAt;

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

    /** URL to the sticker image resource. */
    @JsonProperty("imageUrl")
    public String getImageUrl() {
        return imageUrl;
    }

    /** URL to the sticker image resource. */
    @JsonProperty("imageUrl")
    public void setImageUrl(String imageUrl) {
        this.imageUrl = imageUrl;
    }

    @JsonProperty("createdAt")
    public Date getCreatedAt() {
        return createdAt;
    }

    @JsonProperty("createdAt")
    public void setCreatedAt(Date createdAt) {
        this.createdAt = createdAt;
    }

    @JsonProperty("updatedAt")
    public Date getUpdatedAt() {
        return updatedAt;
    }

    @JsonProperty("updatedAt")
    public void setUpdatedAt(Date updatedAt) {
        this.updatedAt = updatedAt;
    }

    public String getImageKey() {
        return imageKey;
    }

    public void setImageKey(String imageKey) {
        this.imageKey = imageKey;
    }
}
