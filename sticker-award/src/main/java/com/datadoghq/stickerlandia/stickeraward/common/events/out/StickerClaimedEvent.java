package com.datadoghq.stickerlandia.stickeraward.common.events.out;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.common.events.DomainEvent;
import java.util.HashMap;
import java.util.Map;

/**
 * CloudEvent published when a user claims a sticker.
 * Published to the 'users.stickerClaimed.v1' topic.
 */
public class StickerClaimedEvent extends DomainEvent {

    private static final String EVENT_TYPE = "com.datadoghq.stickerlandia.sticker.claimed.v1";
    private static final String EVENT_SOURCE = "/sticker-award-service";

    /** Default constructor for serialization frameworks. */
    public StickerClaimedEvent() {
        super(EVENT_TYPE, EVENT_SOURCE);
    }

    /**
     * Create a new CloudEvent from a sticker assignment entity.
     *
     * @param assignment The sticker assignment entity
     * @return A new event instance
     */
    public static StickerClaimedEvent fromAssignment(StickerAssignment assignment) {
        StickerClaimedEvent event = new StickerClaimedEvent();
        event.setSubject("user/" + assignment.getUserId() 
                + "/sticker/" + assignment.getStickerId());
        
        Map<String, Object> eventData = new HashMap<>();
        eventData.put("accountId", assignment.getUserId());
        eventData.put("stickerId", assignment.getStickerId());
        
        event.setData(eventData);
        return event;
    }
}
