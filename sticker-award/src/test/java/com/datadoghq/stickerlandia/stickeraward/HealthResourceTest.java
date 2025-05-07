package com.datadoghq.stickerlandia.stickeraward;

import io.quarkus.test.junit.QuarkusTest;
import io.restassured.http.ContentType;
import org.junit.jupiter.api.Test;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;
import static org.hamcrest.CoreMatchers.notNullValue;

@QuarkusTest
public class HealthResourceTest {

    @Test
    void testHealthEndpoint() {
        given()
            .when().get("/health")
            .then()
                .statusCode(200)
                .contentType(ContentType.JSON)
                .body("status", is("OK"));

    }
}