package com.datadoghq.stickerlandia.stickeraward.common.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.Map;

/** RFC 7807 Problem Details for HTTP APIs. */
@JsonInclude(JsonInclude.Include.NON_NULL)
public class ProblemDetails {
    @JsonProperty("type")
    private String type;

    @JsonProperty("title")
    private String title;

    @JsonProperty("status")
    private Integer status;

    @JsonProperty("detail")
    private String detail;

    @JsonProperty("instance")
    private String instance;

    private Map<String, Object> additionalProperties;

    /** Default constructor. */
    public ProblemDetails() {}

    /**
     * Constructor with status and title.
     *
     * @param status HTTP status code
     * @param title brief, human-readable message
     */
    public ProblemDetails(int status, String title) {
        this.status = status;
        this.title = title;
    }

    /**
     * Constructor with status, title, and detail.
     *
     * @param status HTTP status code
     * @param title brief, human-readable message
     * @param detail human-readable explanation
     */
    public ProblemDetails(int status, String title, String detail) {
        this.status = status;
        this.title = title;
        this.detail = detail;
    }

    /**
     * Constructor with type, status, title, and detail.
     *
     * @param type URI reference that identifies the problem type
     * @param status HTTP status code
     * @param title brief, human-readable message
     * @param detail human-readable explanation
     */
    public ProblemDetails(String type, int status, String title, String detail) {
        this.type = type;
        this.status = status;
        this.title = title;
        this.detail = detail;
    }

    /**
     * Full constructor with all RFC 7807 fields.
     *
     * @param type URI reference that identifies the problem type
     * @param status HTTP status code
     * @param title brief, human-readable message
     * @param detail human-readable explanation
     * @param instance URI reference that identifies the specific occurrence
     */
    public ProblemDetails(String type, int status, String title, String detail, String instance) {
        this.type = type;
        this.status = status;
        this.title = title;
        this.detail = detail;
        this.instance = instance;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public String getTitle() {
        return title;
    }

    public void setTitle(String title) {
        this.title = title;
    }

    public Integer getStatus() {
        return status;
    }

    public void setStatus(Integer status) {
        this.status = status;
    }

    public String getDetail() {
        return detail;
    }

    public void setDetail(String detail) {
        this.detail = detail;
    }

    public String getInstance() {
        return instance;
    }

    public void setInstance(String instance) {
        this.instance = instance;
    }

    public Map<String, Object> getAdditionalProperties() {
        return additionalProperties;
    }

    public void setAdditionalProperties(Map<String, Object> additionalProperties) {
        this.additionalProperties = additionalProperties;
    }
}
