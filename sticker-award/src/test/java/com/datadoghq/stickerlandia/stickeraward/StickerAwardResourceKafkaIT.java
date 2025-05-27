package com.datadoghq.stickerlandia.stickeraward;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.MethodOrderer;
import org.junit.jupiter.api.Order;
import org.junit.jupiter.api.TestMethodOrder;

import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;

import io.quarkus.test.junit.QuarkusTest;
import jakarta.transaction.Transactional;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response.Status;

/**
 * Integration tests for StickerAwardResource
 * Tests the HTTP API endpoints integration with the database (without Kafka)
 */
@QuarkusTest
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)  
public class StickerAwardResourceKafkaIT {
    
    private static final String TEST_STICKER_ID = "test-sticker-integration";
    private static final String TEST_USER_ID = "test-user-integration";
    
    @Test
    @Transactional
    @Order(1)
    public void testCreateSticker() {
        // Create a test sticker in the database
        Sticker sticker = new Sticker(TEST_STICKER_ID, "Test Sticker", "A test sticker", "https://example.com/sticker.png", 100);
        sticker.persist();
    }
    
    @Test
    @Order(2)
    public void testAssignStickerToUser() {
        // Create the assign sticker command
        AssignStickerCommand command = new AssignStickerCommand();
        command.setStickerId(TEST_STICKER_ID);
        command.setReason("Integration test");
        
        // Call the API to assign the sticker
        given()
            .contentType(MediaType.APPLICATION_JSON)
            .body(command)
            .when()
            .post("/api/award/v1/users/" + TEST_USER_ID + "/stickers")
            .then()
            .statusCode(Status.CREATED.getStatusCode())
            .body("userId", is(TEST_USER_ID))
            .body("stickerId", is(TEST_STICKER_ID));
    }
    
    @Test
    @Order(3)
    public void testGetUserStickers() {
        // Verify the user has the sticker
        given()
            .when()
            .get("/api/award/v1/users/" + TEST_USER_ID + "/stickers")
            .then()
            .statusCode(Status.OK.getStatusCode())
            .body("userId", is(TEST_USER_ID))
            .body("stickers.size()", is(1))
            .body("stickers[0].stickerId", is(TEST_STICKER_ID));
    }
    
    @Test
    @Order(4)
    public void testRemoveStickerFromUser() {
        // Call the API to remove the sticker
        given()
            .when()
            .delete("/api/award/v1/users/" + TEST_USER_ID + "/stickers/" + TEST_STICKER_ID)
            .then()
            .statusCode(Status.OK.getStatusCode())
            .body("userId", is(TEST_USER_ID))
            .body("stickerId", is(TEST_STICKER_ID));
            
        // Verify the user no longer has the sticker
        given()
            .when()
            .get("/api/award/v1/users/" + TEST_USER_ID + "/stickers")
            .then()
            .statusCode(Status.OK.getStatusCode())
            .body("userId", is(TEST_USER_ID))
            .body("stickers.size()", is(0));
    }
}