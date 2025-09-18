/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

/** Response DTO for sticker creation operations. */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"stickerId", "stickerName", "imagePath"})
public class CreateStickerResponse {

    @JsonProperty("stickerId")
    private String stickerId;

    @JsonProperty("stickerName")
    private String stickerName;

    @JsonProperty("imagePath")
    private String imagePath;

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

    @JsonProperty("imagePath")
    public String getImagePath() {
        return imagePath;
    }

    @JsonProperty("imagePath")
    public void setImagePath(String imagePath) {
        this.imagePath = imagePath;
    }
}
