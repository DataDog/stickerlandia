package com.datadoghq.stickerlandia.stickercatalogue;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;
import static org.hamcrest.CoreMatchers.notNullValue;

import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickercatalogue.entity.Sticker;
import io.quarkus.test.junit.QuarkusTest;
import io.restassured.http.ContentType;
import jakarta.inject.Inject;
import jakarta.persistence.EntityManager;
import jakarta.transaction.Transactional;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

@QuarkusTest
class StickerResourceTest {

    private static final String EXISTING_STICKER_ID = "test-sticker-001";
    private static final String NON_EXISTING_STICKER_ID = "non-existing-sticker";

    @Inject EntityManager em;

    @BeforeEach
    @Transactional
    void setupTestData() {
        // Create a test sticker
        Sticker sticker = Sticker.findById(EXISTING_STICKER_ID);
        if (sticker == null) {
            sticker = new Sticker(EXISTING_STICKER_ID, "Test Sticker", "For testing purposes", 100);
            sticker.persist();
        }
    }

    @Test
    void testGetAllStickers() {
        given().when()
                .get("/api/stickers/v1")
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("stickers.size()", is(notNullValue()));
    }

    @Test
    void testCreateSticker() {
        CreateStickerRequest request = new CreateStickerRequest();
        request.setStickerName("New Test Sticker");
        request.setStickerDescription("A new sticker for testing");
        request.setStickerQuantityRemaining(50);

        given().contentType(ContentType.JSON)
                .body(request)
                .when()
                .post("/api/stickers/v1")
                .then()
                .statusCode(201)
                .contentType(ContentType.JSON)
                .body("stickerId", notNullValue())
                .body("stickerName", is("New Test Sticker"));
    }

    @Test
    void testGetExistingStickerMetadata() {
        given().when()
                .get("/api/stickers/v1/{stickerId}", EXISTING_STICKER_ID)
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("stickerId", is(EXISTING_STICKER_ID))
                .body("stickerName", notNullValue());
    }

    @Test
    void testGetNonExistingStickerMetadataReturns404() {
        given().when()
                .get("/api/stickers/v1/{stickerId}", NON_EXISTING_STICKER_ID)
                .then()
                .statusCode(404)
                .contentType("application/problem+json")
                .body("status", is(404))
                .body("title", is("Not Found"))
                .body("detail", is("Sticker with ID " + NON_EXISTING_STICKER_ID + " not found"));
    }

    @Test
    void testUpdateNonExistingStickerReturns404() {
        CreateStickerRequest updateRequest = new CreateStickerRequest();
        updateRequest.setStickerName("Updated Name");

        given().contentType(ContentType.JSON)
                .body(updateRequest)
                .when()
                .put("/api/stickers/v1/{stickerId}", NON_EXISTING_STICKER_ID)
                .then()
                .statusCode(404)
                .contentType("application/problem+json")
                .body("status", is(404))
                .body("title", is("Not Found"))
                .body("detail", is("Sticker with ID " + NON_EXISTING_STICKER_ID + " not found"));
    }

    @Test
    void testDeleteNonExistingStickerReturns404() {
        given().when()
                .delete("/api/stickers/v1/{stickerId}", NON_EXISTING_STICKER_ID)
                .then()
                .statusCode(404)
                .contentType("application/problem+json")
                .body("status", is(404))
                .body("title", is("Not Found"))
                .body("detail", is("Sticker with ID " + NON_EXISTING_STICKER_ID + " not found"));
    }

    @Test
    void testGetImageForNonExistingStickerReturns404() {
        given().when()
                .get("/api/stickers/v1/{stickerId}/image", NON_EXISTING_STICKER_ID)
                .then()
                .statusCode(404)
                .contentType("application/problem+json")
                .body("status", is(404))
                .body("title", is("Not Found"))
                .body("detail", is("No image found for sticker " + NON_EXISTING_STICKER_ID));
    }

    @Test
    void testGetImageForStickerWithoutImageReturns404() {
        given().when()
                .get("/api/stickers/v1/{stickerId}/image", EXISTING_STICKER_ID)
                .then()
                .statusCode(404)
                .contentType("application/problem+json")
                .body("status", is(404))
                .body("title", is("Not Found"))
                .body("detail", is("No image found for sticker " + EXISTING_STICKER_ID));
    }

    @Test
    void testUploadImageForNonExistingStickerReturns404() {
        byte[] fakeImageData = "fake-png-data".getBytes();

        given().contentType("image/png")
                .body(fakeImageData)
                .when()
                .post("/api/stickers/v1/{stickerId}/image", NON_EXISTING_STICKER_ID)
                .then()
                .statusCode(404)
                .contentType("application/problem+json")
                .body("status", is(404))
                .body("title", is("Not Found"))
                .body("detail", is("Sticker with ID " + NON_EXISTING_STICKER_ID + " not found"));
    }
}
