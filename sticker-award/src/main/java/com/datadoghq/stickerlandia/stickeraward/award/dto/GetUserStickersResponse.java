package com.datadoghq.stickerlandia.stickeraward.award.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;
import java.util.ArrayList;
import java.util.List;
import javax.annotation.processing.Generated;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"userId", "stickers"})
@Generated("jsonschema2pojo")
public class GetUserStickersResponse {

    @JsonProperty("userId")
    private String userId;

    @JsonProperty("stickers")
    private List<StickerAssignmentDTO> stickers = new ArrayList<StickerAssignmentDTO>();

    @JsonProperty("userId")
    public String getUserId() {
        return userId;
    }

    @JsonProperty("userId")
    public void setUserId(String userId) {
        this.userId = userId;
    }

    @JsonProperty("stickers")
    public List<StickerAssignmentDTO> getStickers() {
        return stickers;
    }

    @JsonProperty("stickers")
    public void setStickers(List<StickerAssignmentDTO> stickers) {
        this.stickers = stickers;
    }
}
