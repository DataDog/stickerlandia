package com.datadoghq.stickerlandia.stickeraward.common.events;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.Instant;
import java.util.UUID;

/** Base abstract class for all CloudEvents. This class should be extended by all event types. */
public abstract class DomainEvent {

    @JsonProperty("specversion")
    private String specVersion = "1.0";

    @JsonProperty("type")
    private String type;

    @JsonProperty("source")
    private String source;

    @JsonProperty("id")
    private String id;

    @JsonProperty("time")
    private Instant time;

    @JsonProperty("datacontenttype")
    private String dataContentType = "application/json";

    @JsonProperty("subject")
    private String subject;

    @JsonProperty("data")
    private Object data;

    protected DomainEvent(String type, String source) {
        this.type = type;
        this.source = source;
        this.id = UUID.randomUUID().toString();
        this.time = Instant.now();
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

    public Instant getTime() {
        return time;
    }

    public void setTime(Instant time) {
        this.time = time;
    }

    public String getDataContentType() {
        return dataContentType;
    }

    public void setDataContentType(String dataContentType) {
        this.dataContentType = dataContentType;
    }

    public String getSubject() {
        return subject;
    }

    public void setSubject(String subject) {
        this.subject = subject;
    }

    public Object getData() {
        return data;
    }

    public void setData(Object data) {
        this.data = data;
    }
}
