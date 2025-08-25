package com.datadoghq.stickerlandia.stickercatalogue.event;

import com.fasterxml.jackson.annotation.JsonProperty;

/** Base class for domain events following the async API specification pattern */
public abstract class DomainEvent {

    @JsonProperty("eventName")
    private String eventName;

    @JsonProperty("eventVersion")
    private String eventVersion;

    protected DomainEvent() {}

    protected DomainEvent(String eventName, String eventVersion) {
        this.eventName = eventName;
        this.eventVersion = eventVersion;
    }

    public String getEventName() {
        return eventName;
    }

    public void setEventName(String eventName) {
        this.eventName = eventName;
    }

    public String getEventVersion() {
        return eventVersion;
    }

    public void setEventVersion(String eventVersion) {
        this.eventVersion = eventVersion;
    }
}
