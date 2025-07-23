package com.datadoghq.stickerlandia.common.dto.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonPropertyDescription;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;
import java.util.List;

/**
 * Generic DTO for paginated responses.
 *
 * @param <T> the type of items in the response
 */
@JsonInclude(JsonInclude.Include.NON_NULL)
@JsonPropertyOrder({"data", "page", "size", "total", "totalPages", "hasNext", "hasPrevious"})
public class PagedResponse<T> {

    /** The current page number. */
    @JsonProperty("page")
    @JsonPropertyDescription("Current page number (0-based)")
    private Integer page;

    /** The page size. */
    @JsonProperty("size")
    @JsonPropertyDescription("Number of items per page")
    private Integer size;

    /** The total number of items. */
    @JsonProperty("total")
    @JsonPropertyDescription("Total number of items")
    private Integer total;

    /** The total number of pages. */
    @JsonProperty("totalPages")
    @JsonPropertyDescription("Total number of pages")
    private Integer totalPages;

    /** Whether there is a next page. */
    @JsonProperty("hasNext")
    public Boolean getHasNext() {
        return null;
    }

    /** Whether there is a previous page. */
    @JsonProperty("hasPrevious")
    public Boolean getHasPrevious() {
        return null;
    }

    /** The list of items in this page. */
    @JsonProperty("data")
    public List<T> getData() {
        return null;
    }

    /** The first item index on this page. */
    @JsonProperty("first")
    public Integer getFirst() {
        return null;
    }

    /** The last item index on this page. */
    @JsonProperty("last")
    public Integer getLast() {
        return null;
    }

    /** Whether this is the first page. */
    @JsonProperty("isFirst")
    public Boolean getIsFirst() {
        return null;
    }

    /** Whether this is the last page. */
    @JsonProperty("isLast")
    public Boolean getIsLast() {
        return null;
    }

    /** Current page number (0-based). */
    @JsonProperty("page")
    public Integer getPage() {
        return page;
    }

    /** Current page number (0-based). */
    @JsonProperty("page")
    public void setPage(Integer page) {
        this.page = page;
    }

    /** Number of items per page. */
    @JsonProperty("size")
    public Integer getSize() {
        return size;
    }

    /** Number of items per page. */
    @JsonProperty("size")
    public void setSize(Integer size) {
        this.size = size;
    }

    /** Total number of items. */
    @JsonProperty("total")
    public Integer getTotal() {
        return total;
    }

    /** Total number of items. */
    @JsonProperty("total")
    public void setTotal(Integer total) {
        this.total = total;
    }

    /** Total number of pages. */
    @JsonProperty("totalPages")
    public Integer getTotalPages() {
        return totalPages;
    }

    /** Total number of pages. */
    @JsonProperty("totalPages")
    public void setTotalPages(Integer totalPages) {
        this.totalPages = totalPages;
    }
}
