package com.datadoghq.stickerlandia.stickeraward.common.events.out;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.common.events.DomainEvent;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.Instant;

/**
 * Event published when a sticker is removed from a user. Published to the
 * 'stickers.stickerRemovedFromUser.v1' topic.
 */
public class StickerRemovedFromUserEvent extends DomainEvent {

    private static final String EVENT_NAME = "StickerRemovedFromUser";
    private static final String EVENT_VERSION = "v1";

    private String accountId;
    private String stickerId;
    private Instant removedAt;

    /** Default constructor for serialization frameworks. */
    public StickerRemovedFromUserEvent() {
        super(EVENT_NAME, EVENT_VERSION);
    }

    /**
     * Create a new event from a sticker assignment entity.
     *
     * @param assignment The sticker assignment entity with removed status
     * @return A new event instance
     */
    public static StickerRemovedFromUserEvent fromAssignment(StickerAssignment assignment) {
        if (assignment.getRemovedAt() == null) {
            throw new IllegalArgumentException(
                    "Cannot create removal event from active assignment");
        }

        StickerRemovedFromUserEvent event = new StickerRemovedFromUserEvent();
        event.setAccountId(assignment.getUserId());
        event.setStickerId(assignment.getSticker().getStickerId());
        event.setRemovedAt(assignment.getRemovedAt());
        return event;
    }

    /** The ID of the user from whom the sticker was removed. */
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

    /** The ID of the sticker that was removed. */
    @JsonProperty("stickerId")
    public String getStickerId() {
        return stickerId;
    }

    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    public Instant getRemovedAt() {
        return removedAt;
    }

    public void setRemovedAt(Instant removedAt) {
        this.removedAt = removedAt;
    }
}
