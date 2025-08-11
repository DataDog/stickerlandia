package com.datadoghq.stickerlandia.unfortunate.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

public class CreateUnfortunateResponse {

    @JsonProperty("id")
    private String id;

    @JsonProperty("name")
    private String name;

    public CreateUnfortunateResponse() {
    }

    public CreateUnfortunateResponse(String eventIdentifier, String eventTitle) {
        this.id = eventIdentifier;
        this.name = eventTitle;
    }

    @JsonProperty("id")
    public String getId() {
        return id;
    }

    @JsonProperty("id")
    public void setId(String id) {
        this.id = id;
    }

    @JsonProperty("name")
    public String getName() {
        return name;
    }

    @JsonProperty("name")
    public void setName(String name) {
        this.name = name;
    }
}