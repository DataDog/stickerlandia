/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025 Datadog, Inc.
 */
package com.datadoghq.stickerlandia.stickercatalogue.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyDescription;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

/** Request DTO for updating an existing sticker. */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"stickerName", "stickerDescription", "stickerQuantityRemaining"})
public class UpdateStickerRequest {

    @JsonProperty("stickerName")
    private String stickerName;

    @JsonProperty("stickerDescription")
    private String stickerDescription;

    /** Quantity remaining (-1 for infinite). */
    @JsonProperty("stickerQuantityRemaining")
    @JsonPropertyDescription("Quantity remaining (-1 for infinite)")
    private Integer stickerQuantityRemaining;

    /** The name of the sticker. */
    @JsonProperty("stickerName")
    public String getStickerName() {
        return stickerName;
    }

    @JsonProperty("stickerName")
    public void setStickerName(String stickerName) {
        this.stickerName = stickerName;
    }

    /** The description of the sticker. */
    @JsonProperty("stickerDescription")
    public String getStickerDescription() {
        return stickerDescription;
    }

    @JsonProperty("stickerDescription")
    public void setStickerDescription(String stickerDescription) {
        this.stickerDescription = stickerDescription;
    }

    /** The quantity remaining for the sticker. */
    @JsonProperty("stickerQuantityRemaining")
    public Integer getStickerQuantityRemaining() {
        return stickerQuantityRemaining;
    }

    /** Quantity remaining (-1 for infinite). */
    @JsonProperty("stickerQuantityRemaining")
    public void setStickerQuantityRemaining(Integer stickerQuantityRemaining) {
        this.stickerQuantityRemaining = stickerQuantityRemaining;
    }
}
