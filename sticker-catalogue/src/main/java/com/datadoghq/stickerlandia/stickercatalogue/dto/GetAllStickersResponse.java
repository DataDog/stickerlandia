/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025 Datadog, Inc.
 */
package com.datadoghq.stickerlandia.stickercatalogue.dto;

import com.datadoghq.stickerlandia.common.dto.dto.PagedResponse;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;
import java.util.ArrayList;
import java.util.List;

/** Response DTO for getting all stickers. */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"stickers", "pagination"})
public class GetAllStickersResponse {

    @JsonProperty("stickers")
    private List<StickerDTO> stickers = new ArrayList<StickerDTO>();

    @JsonProperty("pagination")
    private PagedResponse pagination;

    @JsonProperty("stickers")
    public List<StickerDTO> getStickers() {
        return stickers;
    }

    @JsonProperty("stickers")
    public void setStickers(List<StickerDTO> stickers) {
        this.stickers = stickers;
    }

    @JsonProperty("pagination")
    public PagedResponse getPagination() {
        return pagination;
    }

    @JsonProperty("pagination")
    public void setPagination(PagedResponse pagination) {
        this.pagination = pagination;
    }
}
