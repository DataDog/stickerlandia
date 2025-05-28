package com.datadoghq.stickerlandia.stickeraward.sticker.dto;

import java.util.ArrayList;
import java.util.List;

import com.datadoghq.stickerlandia.stickeraward.common.dto.PagedResponse;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickers",
    "pagination"
})
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