package com.datadoghq.stickerlandia.stickeraward;

import io.quarkus.test.junit.QuarkusIntegrationTest;
import io.restassured.http.ContentType;
import org.junit.jupiter.api.Test;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;
import static org.hamcrest.CoreMatchers.notNullValue;

@QuarkusIntegrationTest
class StickerAwardResourceIT {
    // Instead of extending StickerAwardResourceTest (which uses @Inject), 
    // implement a simple standalone test
    
    @Test
    void testGetHealth() {
        given()
            .when().get("/health")
            .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("status", is("OK"));
    }
}