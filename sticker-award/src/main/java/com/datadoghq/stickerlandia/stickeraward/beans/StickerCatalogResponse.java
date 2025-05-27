package com.datadoghq.stickerlandia.stickeraward.beans;

import java.util.ArrayList;
import java.util.List;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickers",
    "pagination"
})
public class StickerCatalogResponse {

    @JsonProperty("stickers")
    private List<StickerMetadata> stickers = new ArrayList<StickerMetadata>();
    @JsonProperty("pagination")
    private PagedResponse pagination;

    @JsonProperty("stickers")
    public List<StickerMetadata> getStickers() {
        return stickers;
    }

    @JsonProperty("stickers")
    public void setStickers(List<StickerMetadata> stickers) {
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