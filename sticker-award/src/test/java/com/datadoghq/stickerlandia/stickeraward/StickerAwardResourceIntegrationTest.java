package com.datadoghq.stickerlandia.stickeraward;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;

import io.quarkus.test.junit.QuarkusTest;
import io.restassured.http.ContentType;
import org.junit.jupiter.api.MethodOrderer;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestMethodOrder;

/** Integration test for StickerAwardResource. */
@QuarkusTest
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
public class StickerAwardResourceIntegrationTest {
    // Instead of extending StickerAwardResourceTest (which uses @Inject),
    // implement a simple standalone test

    @Test
    void testGetHealth() {
        given().when()
                .get("/health")
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("status", is("OK"));
    }
}
