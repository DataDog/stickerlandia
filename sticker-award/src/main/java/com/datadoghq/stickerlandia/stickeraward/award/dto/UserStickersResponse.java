
package com.datadoghq.stickerlandia.stickeraward.award.dto;

import java.util.ArrayList;
import java.util.List;
import javax.annotation.processing.Generated;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "userId",
    "stickers"
})
@Generated("jsonschema2pojo")
public class UserStickersResponse {

    @JsonProperty("userId")
    private String userId;
    @JsonProperty("stickers")
    private List<StickerDTO> stickers = new ArrayList<StickerDTO>();

    @JsonProperty("userId")
    public String getUserId() {
        return userId;
    }

    @JsonProperty("userId")
    public void setUserId(String userId) {
        this.userId = userId;
    }

    @JsonProperty("stickers")
    public List<StickerDTO> getStickers() {
        return stickers;
    }

    @JsonProperty("stickers")
    public void setStickers(List<StickerDTO> stickers) {
        this.stickers = stickers;
    }

}
