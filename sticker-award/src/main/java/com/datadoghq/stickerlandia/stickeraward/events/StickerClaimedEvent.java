package com.datadoghq.stickerlandia.stickeraward.events;

import com.datadoghq.stickerlandia.stickeraward.entity.StickerAssignment;

/**
 * Event published when a user claims a sticker.
 * Published to the 'users.stickerClaimed.v1' topic.
 */
public class StickerClaimedEvent extends DomainEvent {
    
    private static final String EVENT_NAME = "StickerClaimed";
    private static final String EVENT_VERSION = "v1";
    
    private String accountId;
    private String stickerId;
    
    /**
     * Default constructor for serialization frameworks
     */
    public StickerClaimedEvent() {
        super(EVENT_NAME, EVENT_VERSION);
    }
    
    /**
     * Create a new event from a sticker assignment entity
     * 
     * @param assignment The sticker assignment entity
     * @return A new event instance
     */
    public static StickerClaimedEvent fromAssignment(StickerAssignment assignment) {
        StickerClaimedEvent event = new StickerClaimedEvent();
        event.setAccountId(assignment.getUserId());
        event.setStickerId(assignment.getSticker().getStickerId());
        return event;
    }
    
    public String getAccountId() {
        return accountId;
    }
    
    public void setAccountId(String accountId) {
        this.accountId = accountId;
    }
    
    public String getStickerId() {
        return stickerId;
    }
    
    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }
}