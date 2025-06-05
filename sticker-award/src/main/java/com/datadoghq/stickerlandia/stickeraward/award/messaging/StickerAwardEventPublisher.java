package com.datadoghq.stickerlandia.stickeraward.award.messaging;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerAssignedToUserEvent;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerClaimedEvent;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerRemovedFromUserEvent;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.inject.Inject;
import org.eclipse.microprofile.reactive.messaging.Channel;
import org.eclipse.microprofile.reactive.messaging.Emitter;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/** Service responsible for publishing sticker-related events to Kafka topics. */
@ApplicationScoped
public class StickerAwardEventPublisher {

    private static final Logger log = LoggerFactory.getLogger(StickerAwardEventPublisher.class);

    @Inject
    @Channel("stickers-assigned")
    Emitter<StickerAssignedToUserEvent> stickerAssignedEmitter;

    @Inject
    @Channel("stickers-removed")
    Emitter<StickerRemovedFromUserEvent> stickerRemovedEmitter;

    @Inject
    @Channel("stickers-claimed")
    Emitter<StickerClaimedEvent> stickerClaimedEmitter;

    /**
     * Publishes a sticker assigned event when a sticker is assigned to a user.
     *
     * @param assignment The sticker assignment entity
     */
    public void publishStickerAssigned(StickerAssignment assignment) {
        try {
            StickerAssignedToUserEvent event =
                    StickerAssignedToUserEvent.fromAssignment(assignment);
            log.info(
                    "Publishing sticker assigned event: userId={}, stickerId={}",
                    event.getAccountId(),
                    event.getStickerId());
            stickerAssignedEmitter.send(event);

            // Also publish sticker claimed event for the user management service
            publishStickerClaimed(assignment);
        } catch (Exception e) {
            log.error("Error publishing sticker assigned event", e);
        }
    }

    /**
     * Publishes a sticker removed event when a sticker is removed from a user.
     *
     * @param assignment The sticker assignment entity with removed status
     */
    public void publishStickerRemoved(StickerAssignment assignment) {
        if (assignment.getRemovedAt() == null) {
            log.warn(
                    "Cannot publish removal event for active assignment: userId={}, stickerId={}",
                    assignment.getUserId(),
                    assignment.getSticker().getStickerId());
            return;
        }

        try {
            StickerRemovedFromUserEvent event =
                    StickerRemovedFromUserEvent.fromAssignment(assignment);
            log.info(
                    "Publishing sticker removed event: userId={}, stickerId={}",
                    event.getAccountId(),
                    event.getStickerId());
            stickerRemovedEmitter.send(event);
        } catch (Exception e) {
            log.error("Error publishing sticker removed event", e);
        }
    }

    /**
     * Publishes a sticker claimed event for the user management service to update claim count.
     *
     * @param assignment The sticker assignment entity
     */
    private void publishStickerClaimed(StickerAssignment assignment) {
        try {
            StickerClaimedEvent event = StickerClaimedEvent.fromAssignment(assignment);
            log.info(
                    "Publishing sticker claimed event: userId={}, stickerId={}",
                    event.getAccountId(),
                    event.getStickerId());
            stickerClaimedEmitter.send(event);
        } catch (Exception e) {
            log.error("Error publishing sticker claimed event", e);
        }
    }
}
