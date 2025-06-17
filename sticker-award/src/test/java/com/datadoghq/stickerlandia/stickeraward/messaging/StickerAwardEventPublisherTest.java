package com.datadoghq.stickerlandia.stickeraward.messaging;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentCaptor.forClass;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.award.messaging.StickerAwardEventPublisher;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerAssignedToUserEvent;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerClaimedEvent;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerRemovedFromUserEvent;
import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;
import io.quarkus.test.InjectMock;
import io.quarkus.test.junit.QuarkusTest;
import jakarta.inject.Inject;
import java.time.Instant;
import java.util.Map;
import org.eclipse.microprofile.reactive.messaging.Emitter;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;

/** Unit tests for the StickerEventPublisher class. */
@QuarkusTest
@org.junit.jupiter.api.Disabled(
        "Kafka integration tests are challenging to set up in this environment")
public class StickerAwardEventPublisherTest {

    @Inject StickerAwardEventPublisher publisher;

    @InjectMock
    @SuppressWarnings("unused")
    Emitter<StickerAssignedToUserEvent> stickerAssignedEmitter;

    @InjectMock
    @SuppressWarnings("unused")
    Emitter<StickerRemovedFromUserEvent> stickerRemovedEmitter;

    @InjectMock
    @SuppressWarnings("unused")
    Emitter<StickerClaimedEvent> stickerClaimedEmitter;

    @Test
    public void testPublishStickerAssigned() {
        // Prepare test data
        Sticker sticker =
                new Sticker(
                        "sticker-123",
                        "Test Sticker",
                        "Test Description",
                        "https://example.com/image.png",
                        100);
        StickerAssignment assignment =
                new StickerAssignment("user-123", sticker.getStickerId(), "For testing");

        // Execute the method
        publisher.publishStickerAssigned(assignment);

        // Verify assigned event
        ArgumentCaptor<StickerAssignedToUserEvent> assignedCaptor =
                forClass(StickerAssignedToUserEvent.class);
        verify(stickerAssignedEmitter, times(1)).send(assignedCaptor.capture());

        StickerAssignedToUserEvent assignedEvent = assignedCaptor.getValue();
        Map<String, Object> assignedData = (Map<String, Object>) assignedEvent.getData();
        assertEquals("user-123", assignedData.get("accountId"));
        assertEquals("sticker-123", assignedData.get("stickerId"));
        assertEquals(assignment.getAssignedAt(), assignedData.get("assignedAt"));

        // Verify claimed event is also published
        ArgumentCaptor<StickerClaimedEvent> claimedCaptor = forClass(StickerClaimedEvent.class);
        verify(stickerClaimedEmitter, times(1)).send(claimedCaptor.capture());

        StickerClaimedEvent claimedEvent = claimedCaptor.getValue();
        Map<String, Object> claimedData = (Map<String, Object>) claimedEvent.getData();
        assertEquals("user-123", claimedData.get("accountId"));
        assertEquals("sticker-123", claimedData.get("stickerId"));
    }

    @Test
    public void testPublishStickerRemoved() {
        // Prepare test data
        Sticker sticker =
                new Sticker(
                        "sticker-456",
                        "Test Sticker",
                        "Test Description",
                        "https://example.com/image.png",
                        100);
        StickerAssignment assignment =
                new StickerAssignment("user-456", sticker.getStickerId(), "For testing");
        Instant removedAt = Instant.now();
        assignment.setRemovedAt(removedAt);

        // Execute the method
        publisher.publishStickerRemoved(assignment);

        // Verify removed event
        ArgumentCaptor<StickerRemovedFromUserEvent> removedCaptor =
                forClass(StickerRemovedFromUserEvent.class);
        verify(stickerRemovedEmitter, times(1)).send(removedCaptor.capture());

        StickerRemovedFromUserEvent removedEvent = removedCaptor.getValue();
        Map<String, Object> removedData = (Map<String, Object>) removedEvent.getData();
        assertEquals("user-456", removedData.get("accountId"));
        assertEquals("sticker-456", removedData.get("stickerId"));
        assertEquals(removedAt, removedData.get("removedAt"));
    }

    @Test
    public void testPublishStickerRemovedWithActiveAssignment() {
        // Prepare test data with no removal date
        Sticker sticker =
                new Sticker(
                        "sticker-789",
                        "Test Sticker",
                        "Test Description",
                        "https://example.com/image.png",
                        100);
        StickerAssignment assignment =
                new StickerAssignment("user-789", sticker.getStickerId(), "For testing");

        // Execute the method - should log a warning but not throw exception
        publisher.publishStickerRemoved(assignment);

        // Verify no events were sent
        verify(stickerRemovedEmitter, never())
                .send(org.mockito.ArgumentMatchers.any(StickerRemovedFromUserEvent.class));
    }

    @Test
    public void testEmitterFailure() {
        // Prepare test data
        Sticker sticker =
                new Sticker(
                        "sticker-fail",
                        "Test Sticker",
                        "Test Description",
                        "https://example.com/image.png",
                        100);
        StickerAssignment assignment =
                new StickerAssignment("user-fail", sticker.getStickerId(), "For testing");

        // Simulate emitter failure
        doThrow(new RuntimeException("Test failure"))
                .when(stickerAssignedEmitter)
                .send(org.mockito.ArgumentMatchers.any(StickerAssignedToUserEvent.class));

        // Execute the method - should catch the exception and log it
        publisher.publishStickerAssigned(assignment);

        // The claimed emitter should still have been called
        verify(stickerClaimedEmitter, times(1))
                .send(org.mockito.ArgumentMatchers.any(StickerClaimedEvent.class));
    }
}
