package com.datadoghq.stickerlandia.stickeraward.sticker.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyDescription;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

/** Request DTO for creating a new sticker. */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"stickerName", "stickerDescription", "stickerQuantityRemaining"})
public class CreateStickerRequest {

    @JsonProperty("stickerName")
    private String stickerName;

    @JsonProperty("stickerDescription")
    private String stickerDescription;

    @JsonProperty("stickerQuantityRemaining")
    @JsonPropertyDescription("Quantity remaining (-1 for infinite)")
    private Integer stickerQuantityRemaining;

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

    @JsonProperty("stickerQuantityRemaining")
    public Integer getStickerQuantityRemaining() {
        return stickerQuantityRemaining;
    }

    @JsonProperty("stickerQuantityRemaining")
    public void setStickerQuantityRemaining(Integer stickerQuantityRemaining) {
        this.stickerQuantityRemaining = stickerQuantityRemaining;
    }
}
