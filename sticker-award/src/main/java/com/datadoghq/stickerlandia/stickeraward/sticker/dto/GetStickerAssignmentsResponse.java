package com.datadoghq.stickerlandia.stickeraward.sticker.dto;

import java.util.ArrayList;
import java.util.List;

import com.datadoghq.stickerlandia.stickeraward.common.dto.PagedResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.UserAssignmentDTO;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickerId",
    "assignments",
    "pagination"
})
public class GetStickerAssignmentsResponse {

    @JsonProperty("stickerId")
    private String stickerId;
    @JsonProperty("assignments")
    private List<UserAssignmentDTO> assignments = new ArrayList<UserAssignmentDTO>();
    @JsonProperty("pagination")
    private PagedResponse pagination;

    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    @JsonProperty("stickerId")
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    @JsonProperty("assignments")
    public List<UserAssignmentDTO> getAssignments() {
        return assignments;
    }

    @JsonProperty("assignments")
    public void setAssignments(List<UserAssignmentDTO> assignments) {
        this.assignments = assignments;
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