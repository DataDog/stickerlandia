package com.datadoghq.stickerlandia.stickeraward.common.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyDescription;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({
    "page",
    "size",
    "total",
    "totalPages"
})
public class PagedResponse {

    /**
     * Current page number (0-based)
     * 
     */
    @JsonProperty("page")
    @JsonPropertyDescription("Current page number (0-based)")
    private Integer page;
    /**
     * Number of items per page
     * 
     */
    @JsonProperty("size")
    @JsonPropertyDescription("Number of items per page")
    private Integer size;
    /**
     * Total number of items
     * 
     */
    @JsonProperty("total")
    @JsonPropertyDescription("Total number of items")
    private Integer total;
    /**
     * Total number of pages
     * 
     */
    @JsonProperty("totalPages")
    @JsonPropertyDescription("Total number of pages")
    private Integer totalPages;

    /**
     * Current page number (0-based)
     * 
     */
    @JsonProperty("page")
    public Integer getPage() {
        return page;
    }

    /**
     * Current page number (0-based)
     * 
     */
    @JsonProperty("page")
    public void setPage(Integer page) {
        this.page = page;
    }

    /**
     * Number of items per page
     * 
     */
    @JsonProperty("size")
    public Integer getSize() {
        return size;
    }

    /**
     * Number of items per page
     * 
     */
    @JsonProperty("size")
    public void setSize(Integer size) {
        this.size = size;
    }

    /**
     * Total number of items
     * 
     */
    @JsonProperty("total")
    public Integer getTotal() {
        return total;
    }

    /**
     * Total number of items
     * 
     */
    @JsonProperty("total")
    public void setTotal(Integer total) {
        this.total = total;
    }

    /**
     * Total number of pages
     * 
     */
    @JsonProperty("totalPages")
    public Integer getTotalPages() {
        return totalPages;
    }

    /**
     * Total number of pages
     * 
     */
    @JsonProperty("totalPages")
    public void setTotalPages(Integer totalPages) {
        this.totalPages = totalPages;
    }

}