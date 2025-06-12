package com.datadoghq.stickerlandia.stickeraward.common.events.out;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.common.events.DomainEvent;
import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Event published when a user claims a sticker. Published to the 'users.stickerClaimed.v1' topic.
 */
public class StickerClaimedEvent extends DomainEvent {

    private static final String EVENT_NAME = "StickerClaimed";
    private static final String EVENT_VERSION = "v1";

    private String accountId;
    private String stickerId;

    /** Default constructor for serialization frameworks. */
    public StickerClaimedEvent() {
        super(EVENT_NAME, EVENT_VERSION);
    }

    /**
     * Create a new event from a sticker assignment entity.
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

    /** The ID of the user who claimed the sticker. */
    @JsonProperty("userId")
    public String getUserId() {
        return accountId;
    }

    public void setAccountId(String accountId) {
        this.accountId = accountId;
    }

    public String getAccountId() {
        return accountId;
    }

    /** The ID of the sticker that was claimed. */
    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }
}
