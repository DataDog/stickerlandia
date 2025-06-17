package com.datadoghq.stickerlandia.stickeraward.common.events.out;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.common.events.DomainEvent;
import java.time.Instant;
import java.util.HashMap;
import java.util.Map;

/**
 * CloudEvent published when a sticker is assigned to a user. Published to the
 * 'stickers.stickerAssignedToUser.v1' topic.
 */
public class StickerAssignedToUserEvent extends DomainEvent {

    private static final String EVENT_TYPE = "com.datadoghq.stickerlandia.sticker.assigned.v1";
    private static final String EVENT_SOURCE = "/sticker-award-service";

    /** Default constructor for serialization frameworks. */
    public StickerAssignedToUserEvent() {
        super(EVENT_TYPE, EVENT_SOURCE);
    }

    /**
     * Create a new CloudEvent from a sticker assignment entity.
     *
     * @param assignment The sticker assignment entity
     * @return A new event instance
     */
    public static StickerAssignedToUserEvent fromAssignment(StickerAssignment assignment) {
        StickerAssignedToUserEvent event = new StickerAssignedToUserEvent();
        event.setSubject("user/" + assignment.getUserId() 
                + "/sticker/" + assignment.getStickerId());
        
        Map<String, Object> eventData = new HashMap<>();
        eventData.put("accountId", assignment.getUserId());
        eventData.put("stickerId", assignment.getStickerId());
        eventData.put("assignedAt", assignment.getAssignedAt());
        
        event.setData(eventData);
        return event;
    }
}
