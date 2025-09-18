/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue.event;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.Instant;
import java.util.UUID;

/**
 * CloudEvents v1.0 specification compliant wrapper with W3C trace context support
 *
 * @param <T> The type of the event data
 */
public class CloudEvent<T> {

    @JsonProperty("specversion")
    private String specVersion = "1.0";

    @JsonProperty("type")
    private String type;

    @JsonProperty("source")
    private String source;

    @JsonProperty("id")
    private String id;

    @JsonProperty("time")
    private String time;

    @JsonProperty("traceparent")
    private String traceParent;

    @JsonProperty("data")
    private T data;

    public CloudEvent() {
        this.id = UUID.randomUUID().toString();
        this.time = Instant.now().toString();
    }

    public CloudEvent(String type, String source, T data) {
        this();
        this.type = type;
        this.source = source;
        this.data = data;
    }

    public String getSpecVersion() {
        return specVersion;
    }

    public void setSpecVersion(String specVersion) {
        this.specVersion = specVersion;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public String getSource() {
        return source;
    }

    public void setSource(String source) {
        this.source = source;
    }

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getTime() {
        return time;
    }

    public void setTime(Instant time) {
        this.time = time.toString();
    }

    public String getTraceParent() {
        return traceParent;
    }

    public void setTraceParent(String traceParent) {
        this.traceParent = traceParent;
    }

    public T getData() {
        return data;
    }

    public void setData(T data) {
        this.data = data;
    }
}
