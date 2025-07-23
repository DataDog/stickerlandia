package com.datadoghq.stickerlandia.stickercatalogue;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;

import io.quarkus.test.junit.QuarkusTest;
import io.restassured.http.ContentType;
import org.junit.jupiter.api.Test;

/** Test for the health resource. */
@QuarkusTest
public class HealthResourceTest {

    @Test
    void testHealthEndpoint() {
        given().when()
                .get("/health")
                .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("status", is("OK"));
    }
}
