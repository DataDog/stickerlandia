/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025 Datadog, Inc.
 */
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
