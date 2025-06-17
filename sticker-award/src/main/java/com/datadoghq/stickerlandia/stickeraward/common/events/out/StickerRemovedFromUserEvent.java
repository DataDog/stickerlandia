package com.datadoghq.stickerlandia.stickeraward.common.events.out;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.common.events.DomainEvent;
import java.util.HashMap;
import java.util.Map;

/**
 * CloudEvent published when a sticker is removed from a user. Published to the
 * 'stickers.stickerRemovedFromUser.v1' topic.
 */
public class StickerRemovedFromUserEvent extends DomainEvent {

    private static final String EVENT_TYPE = "com.datadoghq.stickerlandia.sticker.removed.v1";
    private static final String EVENT_SOURCE = "/sticker-award-service";

    /** Default constructor for serialization frameworks. */
    public StickerRemovedFromUserEvent() {
        super(EVENT_TYPE, EVENT_SOURCE);
    }

    /**
     * Create a new CloudEvent from a sticker assignment entity.
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
        event.setSubject("user/" + assignment.getUserId() 
                + "/sticker/" + assignment.getStickerId());
        
        Map<String, Object> eventData = new HashMap<>();
        eventData.put("accountId", assignment.getUserId());
        eventData.put("stickerId", assignment.getStickerId());
        eventData.put("removedAt", assignment.getRemovedAt());
        
        event.setData(eventData);
        return event;
    }
}
