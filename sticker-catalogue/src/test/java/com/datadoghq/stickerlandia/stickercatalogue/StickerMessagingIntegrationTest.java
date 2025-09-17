/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025 Datadog, Inc.
 */
package com.datadoghq.stickerlandia.stickercatalogue;

import static io.restassured.RestAssured.given;
import static org.awaitility.Awaitility.await;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickercatalogue.entity.Sticker;
import com.datadoghq.stickerlandia.stickercatalogue.event.CloudEvent;
import com.datadoghq.stickerlandia.stickercatalogue.event.StickerAddedEvent;
import com.datadoghq.stickerlandia.stickercatalogue.event.StickerDeletedEvent;
import com.datadoghq.stickerlandia.stickercatalogue.event.StickerUpdatedEvent;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.quarkus.test.junit.QuarkusTest;
import io.quarkus.test.junit.TestProfile;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import jakarta.inject.Inject;
import jakarta.persistence.EntityManager;
import jakarta.transaction.Transactional;
import java.time.Duration;
import java.util.concurrent.CopyOnWriteArrayList;
import org.eclipse.microprofile.reactive.messaging.Incoming;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

@QuarkusTest
@TestProfile(KafkaTestProfile.class)
class StickerMessagingIntegrationTest {

    private static final String TEST_STICKER_ID = "test-messaging-sticker-001";

    @Inject EntityManager em;

    @Inject ObjectMapper objectMapper;

    // Test message collectors
    private static final CopyOnWriteArrayList<CloudEvent<StickerAddedEvent>> addedEvents =
            new CopyOnWriteArrayList<>();
    private static final CopyOnWriteArrayList<CloudEvent<StickerUpdatedEvent>> updatedEvents =
            new CopyOnWriteArrayList<>();
    private static final CopyOnWriteArrayList<CloudEvent<StickerDeletedEvent>> deletedEvents =
            new CopyOnWriteArrayList<>();

    @BeforeEach
    @Transactional
    void setupTest() {
        // Clean up any existing test sticker
        Sticker existing = Sticker.findById(TEST_STICKER_ID);
        if (existing != null) {
            existing.delete();
        }

        // Clear message collectors
        addedEvents.clear();
        updatedEvents.clear();
        deletedEvents.clear();
    }

    // Kafka message consumers for testing
    @Incoming("stickers_added_test")
    void consumeStickerAddedEvent(String message) {
        try {
            CloudEvent<StickerAddedEvent> event =
                    objectMapper.readValue(
                            message, new TypeReference<CloudEvent<StickerAddedEvent>>() {});
            addedEvents.add(event);
        } catch (Exception e) {
            throw new RuntimeException("Failed to parse StickerAddedEvent", e);
        }
    }

    @Incoming("stickers_updated_test")
    void consumeStickerUpdatedEvent(String message) {
        try {
            CloudEvent<StickerUpdatedEvent> event =
                    objectMapper.readValue(
                            message, new TypeReference<CloudEvent<StickerUpdatedEvent>>() {});
            updatedEvents.add(event);
        } catch (Exception e) {
            throw new RuntimeException("Failed to parse StickerUpdatedEvent", e);
        }
    }

    @Incoming("stickers_deleted_test")
    void consumeStickerDeletedEvent(String message) {
        try {
            CloudEvent<StickerDeletedEvent> event =
                    objectMapper.readValue(
                            message, new TypeReference<CloudEvent<StickerDeletedEvent>>() {});
            deletedEvents.add(event);
        } catch (Exception e) {
            throw new RuntimeException("Failed to parse StickerDeletedEvent", e);
        }
    }

    @Test
    void testCreateStickerPublishesKafkaEvent() {
        CreateStickerRequest request = new CreateStickerRequest();
        request.setStickerName("Test Messaging Sticker");
        request.setStickerDescription("A sticker for testing Kafka messaging");
        request.setStickerQuantityRemaining(100);

        Response response =
                given().contentType(ContentType.JSON)
                        .body(request)
                        .when()
                        .post("/api/stickers/v1")
                        .then()
                        .statusCode(201)
                        .extract()
                        .response();

        String stickerId = response.jsonPath().getString("stickerId");
        assertNotNull(stickerId);

        // Wait for Kafka message
        await().atMost(Duration.ofSeconds(10))
                .untilAsserted(
                        () -> {
                            assertEquals(
                                    1,
                                    addedEvents.size(),
                                    "Should receive exactly one StickerAddedEvent");
                        });

        CloudEvent<StickerAddedEvent> cloudEvent = addedEvents.get(0);

        // Verify CloudEvent wrapper
        assertEquals("1.0", cloudEvent.getSpecVersion());
        assertEquals("stickers.stickerAdded.v1", cloudEvent.getType());
        assertNotNull(cloudEvent.getId());
        assertNotNull(cloudEvent.getTime());
        assertNotNull(cloudEvent.getSource());

        // Verify event data
        StickerAddedEvent eventData = cloudEvent.getData();
        assertEquals(stickerId, eventData.getStickerId());
        assertEquals("Test Messaging Sticker", eventData.getName());
        assertEquals("A sticker for testing Kafka messaging", eventData.getDescription());
        assertNotNull(eventData.getAddedAt());
    }

    @Test
    void testUpdateStickerPublishesKafkaEvent() {
        // First create a sticker
        createTestSticker();

        CreateStickerRequest updateRequest = new CreateStickerRequest();
        updateRequest.setStickerName("Updated Test Sticker");
        updateRequest.setStickerDescription("Updated description");
        updateRequest.setStickerQuantityRemaining(150);

        given().contentType(ContentType.JSON)
                .body(updateRequest)
                .when()
                .put("/api/stickers/v1/{stickerId}", TEST_STICKER_ID)
                .then()
                .statusCode(200);

        // Wait for Kafka message
        await().atMost(Duration.ofSeconds(10))
                .untilAsserted(
                        () -> {
                            assertEquals(
                                    1,
                                    updatedEvents.size(),
                                    "Should receive exactly one StickerUpdatedEvent");
                        });

        CloudEvent<StickerUpdatedEvent> cloudEvent = updatedEvents.get(0);

        // Verify CloudEvent wrapper
        assertEquals("1.0", cloudEvent.getSpecVersion());
        assertEquals("stickers.stickerUpdated.v1", cloudEvent.getType());
        assertNotNull(cloudEvent.getId());
        assertNotNull(cloudEvent.getTime());

        // Verify event data
        StickerUpdatedEvent eventData = cloudEvent.getData();
        assertEquals(TEST_STICKER_ID, eventData.getStickerId());
        assertEquals("Updated Test Sticker", eventData.getName());
        assertEquals("Updated description", eventData.getDescription());
        assertNotNull(eventData.getUpdatedAt());
    }

    @Test
    void testDeleteStickerPublishesKafkaEvent() {
        // First create a sticker
        createTestSticker();

        given().when()
                .delete("/api/stickers/v1/{stickerId}", TEST_STICKER_ID)
                .then()
                .statusCode(204);

        // Wait for Kafka message
        await().atMost(Duration.ofSeconds(10))
                .untilAsserted(
                        () -> {
                            assertEquals(
                                    1,
                                    deletedEvents.size(),
                                    "Should receive exactly one StickerDeletedEvent");
                        });

        CloudEvent<StickerDeletedEvent> cloudEvent = deletedEvents.get(0);

        // Verify CloudEvent wrapper
        assertEquals("1.0", cloudEvent.getSpecVersion());
        assertEquals("stickers.stickerDeleted.v1", cloudEvent.getType());
        assertNotNull(cloudEvent.getId());
        assertNotNull(cloudEvent.getTime());

        // Verify event data
        StickerDeletedEvent eventData = cloudEvent.getData();
        assertEquals(TEST_STICKER_ID, eventData.getStickerId());
        assertEquals("Test Messaging Sticker", eventData.getName());
        assertNotNull(eventData.getDeletedAt());
    }

    @Transactional
    void createTestSticker() {
        Sticker sticker =
                new Sticker(
                        TEST_STICKER_ID,
                        "Test Messaging Sticker",
                        "A sticker for testing messaging",
                        100);
        sticker.persist();

        // Clear any events from creation to focus on the update/delete tests
        addedEvents.clear();
        updatedEvents.clear();
        deletedEvents.clear();
    }
}
