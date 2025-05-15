package com.datadoghq.stickerlandia.stickeraward.events;

import java.time.Instant;

import com.datadoghq.stickerlandia.stickeraward.entity.StickerAssignment;

/**
 * Event published when a sticker is assigned to a user.
 * Published to the 'stickers.stickerAssignedToUser.v1' topic.
 */
public class StickerAssignedToUserEvent extends DomainEvent {
    
    private static final String EVENT_NAME = "StickerAssignedToUser";
    private static final String EVENT_VERSION = "v1";
    
    private String accountId;
    private String stickerId;
    private Instant assignedAt;
    
    /**
     * Default constructor for serialization frameworks
     */
    public StickerAssignedToUserEvent() {
        super(EVENT_NAME, EVENT_VERSION);
    }
    
    /**
     * Create a new event from a sticker assignment entity
     * 
     * @param assignment The sticker assignment entity
     * @return A new event instance
     */
    public static StickerAssignedToUserEvent fromAssignment(StickerAssignment assignment) {
        StickerAssignedToUserEvent event = new StickerAssignedToUserEvent();
        event.setAccountId(assignment.getUserId());
        event.setStickerId(assignment.getSticker().getStickerId());
        event.setAssignedAt(assignment.getAssignedAt());
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
    
    public Instant getAssignedAt() {
        return assignedAt;
    }
    
    public void setAssignedAt(Instant assignedAt) {
        this.assignedAt = assignedAt;
    }
}