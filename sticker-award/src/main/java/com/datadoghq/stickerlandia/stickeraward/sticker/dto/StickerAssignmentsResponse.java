package com.datadoghq.stickerlandia.stickeraward.sticker.dto;

import java.util.ArrayList;
import java.util.List;

import com.datadoghq.stickerlandia.stickeraward.common.dto.PagedResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.UserAssignment;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "stickerId",
    "assignments",
    "pagination"
})
public class StickerAssignmentsResponse {

    @JsonProperty("stickerId")
    private String stickerId;
    @JsonProperty("assignments")
    private List<UserAssignment> assignments = new ArrayList<UserAssignment>();
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
    public List<UserAssignment> getAssignments() {
        return assignments;
    }

    @JsonProperty("assignments")
    public void setAssignments(List<UserAssignment> assignments) {
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