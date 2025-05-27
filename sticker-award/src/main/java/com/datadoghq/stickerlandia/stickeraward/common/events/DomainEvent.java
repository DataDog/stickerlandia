package com.datadoghq.stickerlandia.stickeraward.common.events;

/**
 * Base abstract class for all domain events.
 * This class should be extended by all event types.
 */
public abstract class DomainEvent {
    
    private String eventName;
    private String eventVersion;
    
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