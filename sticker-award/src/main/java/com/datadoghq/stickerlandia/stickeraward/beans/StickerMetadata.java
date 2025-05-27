package com.datadoghq.stickerlandia.stickeraward.beans;

import java.util.Date;
import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyDescription;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

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
public class StickerMetadata {

    @JsonProperty("stickerId")
    private String stickerId;
    @JsonProperty("stickerName")
    private String stickerName;
    @JsonProperty("stickerDescription")
    private String stickerDescription;
    /**
     * Quantity remaining (-1 for infinite)
     * 
     */
    @JsonProperty("stickerQuantityRemaining")
    @JsonPropertyDescription("Quantity remaining (-1 for infinite)")
    private Integer stickerQuantityRemaining;
    /**
     * URL to the sticker image resource
     * 
     */
    @JsonProperty("imageUrl")
    @JsonPropertyDescription("URL to the sticker image resource")
    private String imageUrl;
    @JsonFormat(shape = JsonFormat.Shape.STRING, pattern = "yyyy-MM-dd'T'HH:mm:ss'Z'", timezone = "UTC")
    @JsonProperty("createdAt")
    private Date createdAt;
    @JsonFormat(shape = JsonFormat.Shape.STRING, pattern = "yyyy-MM-dd'T'HH:mm:ss'Z'", timezone = "UTC")
    @JsonProperty("updatedAt")
    private Date updatedAt;

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

    @JsonProperty("stickerDescription")
    public String getStickerDescription() {
        return stickerDescription;
    }

    @JsonProperty("stickerDescription")
    public void setStickerDescription(String stickerDescription) {
        this.stickerDescription = stickerDescription;
    }

    /**
     * Quantity remaining (-1 for infinite)
     * 
     */
    @JsonProperty("stickerQuantityRemaining")
    public Integer getStickerQuantityRemaining() {
        return stickerQuantityRemaining;
    }

    /**
     * Quantity remaining (-1 for infinite)
     * 
     */
    @JsonProperty("stickerQuantityRemaining")
    public void setStickerQuantityRemaining(Integer stickerQuantityRemaining) {
        this.stickerQuantityRemaining = stickerQuantityRemaining;
    }

    /**
     * URL to the sticker image resource
     * 
     */
    @JsonProperty("imageUrl")
    public String getImageUrl() {
        return imageUrl;
    }

    /**
     * URL to the sticker image resource
     * 
     */
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

}