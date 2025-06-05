package com.datadoghq.stickerlandia.stickeraward;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;
import static org.hamcrest.CoreMatchers.notNullValue;

import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;
import io.quarkus.test.junit.QuarkusTest;
import io.restassured.http.ContentType;
import jakarta.inject.Inject;
import jakarta.persistence.EntityManager;
import jakarta.transaction.Transactional;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

@QuarkusTest
class StickerAwardResourceTest {

    private static final String TEST_USER_ID = "user-001";
    private static final String EXISTING_STICKER_ID = "sticker-001";
    private static final String NON_EXISTING_STICKER_ID = "sticker-999";

    @Inject EntityManager em;

    @BeforeEach
    @Transactional
    void setupTestData() {
        // Make sure our test stickers exist
        Sticker sticker1 = Sticker.findById(EXISTING_STICKER_ID);
        if (sticker1 == null) {
            sticker1 =
                    new Sticker(
                            EXISTING_STICKER_ID,
                            "Test Sticker",
                            "For testing",
                            "http://example.com/test.png",
                            100);
            sticker1.persist();
        }

        // Create sticker-002 and sticker-003 for other tests
        Sticker sticker2 = Sticker.findById("sticker-002");
        if (sticker2 == null) {
            sticker2 =
                    new Sticker(
                            "sticker-002",
                            "Test Sticker 2",
                            "For testing",
                            "http://example.com/test2.png",
                            100);
            sticker2.persist();
        }

        Sticker sticker3 = Sticker.findById("sticker-003");
        if (sticker3 == null) {
            sticker3 =
                    new Sticker(
                            "sticker-003",
                            "Test Sticker 3",
                            "For testing",
                            "http://example.com/test3.png",
                            100);
            sticker3.persist();
        }

        // Make sure user has a sticker assignment
        StickerAssignment assignment =
                StickerAssignment.findActiveByUserAndSticker(TEST_USER_ID, EXISTING_STICKER_ID);
        if (assignment == null) {
            assignment = new StickerAssignment(TEST_USER_ID, sticker1, "For test setup");
            assignment.persist();
        }
    }

    @Test
    void testGetUserStickers() {
        given().when()
                .get("/api/award/v1/users/{userId}/stickers", TEST_USER_ID)
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("userId", is(TEST_USER_ID))
                .body("stickers.size()", is(1))
                .body("stickers[0].stickerId", is(EXISTING_STICKER_ID));
    }

    @Test
    void testGetUserStickersForUserWithNoStickers() {
        String unknownUserId = "unknown-user";
        given().when()
                .get("/api/award/v1/users/{userId}/stickers", unknownUserId)
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("userId", is(unknownUserId))
                .body("stickers.size()", is(0));
    }

    @Test
    void testAssignStickerToUser() {
        String userId = "test-user-" + System.currentTimeMillis();
        String stickerId = "sticker-002"; // Use one of the stickers from our seed data

        // Create sticker assignment request using a proper bean for serialization
        AssignStickerRequest command = new AssignStickerRequest();
        command.setStickerId(stickerId);
        command.setReason("Test assignment");

        given().contentType(ContentType.JSON)
                .body(command)
                .when()
                .post("/api/award/v1/users/{userId}/stickers", userId)
                .then()
                .statusCode(201)
                .contentType(ContentType.JSON)
                .body("userId", is(userId))
                .body("stickerId", is(stickerId))
                .body("assignedAt", notNullValue());

        // Verify the sticker is now assigned by getting user stickers
        given().when()
                .get("/api/award/v1/users/{userId}/stickers", userId)
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("stickers.size()", is(1))
                .body("stickers[0].stickerId", is(stickerId));
    }

    @Test
    void testAssignNonExistingStickerReturns400() {
        String userId = "test-user-" + System.currentTimeMillis();

        // Create sticker assignment request with non-existing sticker
        AssignStickerRequest command = new AssignStickerRequest();
        command.setStickerId(NON_EXISTING_STICKER_ID);
        command.setReason("Test assignment");

        given().contentType(ContentType.JSON)
                .body(command)
                .when()
                .post("/api/award/v1/users/{userId}/stickers", userId)
                .then()
                .statusCode(400);
    }

    @Test
    void testAssignAlreadyAssignedStickerReturns409() {
        // First, assign a sticker
        String userId = "test-user-" + System.currentTimeMillis();
        String stickerId = "sticker-003"; // Use one from seed data

        AssignStickerRequest command = new AssignStickerRequest();
        command.setStickerId(stickerId);
        command.setReason("Test assignment");

        // First assignment should succeed
        given().contentType(ContentType.JSON)
                .body(command)
                .when()
                .post("/api/award/v1/users/{userId}/stickers", userId)
                .then()
                .statusCode(201);

        // Second assignment of the same sticker should fail with 409 Conflict
        given().contentType(ContentType.JSON)
                .body(command)
                .when()
                .post("/api/award/v1/users/{userId}/stickers", userId)
                .then()
                .statusCode(409);
    }

    @Test
    void testRemoveStickerAssignment() {
        // First, assign a sticker
        String userId = "test-user-" + System.currentTimeMillis();
        String stickerId = "sticker-002";

        AssignStickerRequest command = new AssignStickerRequest();
        command.setStickerId(stickerId);
        command.setReason("Test assignment");

        // Assign the sticker
        given().contentType(ContentType.JSON)
                .body(command)
                .when()
                .post("/api/award/v1/users/{userId}/stickers", userId)
                .then()
                .statusCode(201);

        // Remove the sticker
        given().when()
                .delete("/api/award/v1/users/{userId}/stickers/{stickerId}", userId, stickerId)
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("userId", is(userId))
                .body("stickerId", is(stickerId))
                .body("removedAt", notNullValue());

        // Verify the sticker is no longer assigned
        given().when()
                .get("/api/award/v1/users/{userId}/stickers", userId)
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("stickers.size()", is(0));
    }

    @Test
    void testRemoveNonExistingStickerAssignmentReturns404() {
        String userId = "test-user-" + System.currentTimeMillis();
        String stickerId = "sticker-001";

        given().when()
                .delete("/api/award/v1/users/{userId}/stickers/{stickerId}", userId, stickerId)
                .then()
                .statusCode(404);
    }
}
