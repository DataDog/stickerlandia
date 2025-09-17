/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025 Datadog, Inc.
 */
package com.datadoghq.stickerlandia.stickercatalogue.common.exception;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

import com.datadoghq.stickerlandia.common.dto.dto.ProblemDetails;
import com.datadoghq.stickerlandia.common.dto.exception.ProblemDetailsExceptionMapper;
import jakarta.ws.rs.BadRequestException;
import jakarta.ws.rs.ForbiddenException;
import jakarta.ws.rs.NotFoundException;
import jakarta.ws.rs.WebApplicationException;
import jakarta.ws.rs.core.Response;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ProblemDetailsExceptionMapperTest {

    private ProblemDetailsExceptionMapper mapper;

    @BeforeEach
    void setUp() {
        mapper = new ProblemDetailsExceptionMapper();
    }

    @Test
    void testMapBadRequestException() {
        WebApplicationException exception = new BadRequestException("Invalid input data");

        Response response = mapper.toResponse(exception);

        assertEquals(400, response.getStatus());
        assertEquals("application/problem+json", response.getHeaders().getFirst("Content-Type"));

        ProblemDetails problemDetails = (ProblemDetails) response.getEntity();
        assertNotNull(problemDetails);
        assertEquals(400, problemDetails.getStatus());
        assertEquals("Bad Request", problemDetails.getTitle());
        assertEquals("Invalid input data", problemDetails.getDetail());
    }

    @Test
    void testMapNotFoundException() {
        WebApplicationException exception = new NotFoundException("Resource not found");

        Response response = mapper.toResponse(exception);

        assertEquals(404, response.getStatus());
        assertEquals("application/problem+json", response.getHeaders().getFirst("Content-Type"));

        ProblemDetails problemDetails = (ProblemDetails) response.getEntity();
        assertNotNull(problemDetails);
        assertEquals(404, problemDetails.getStatus());
        assertEquals("Not Found", problemDetails.getTitle());
        assertEquals("Resource not found", problemDetails.getDetail());
    }

    @Test
    void testMapForbiddenException() {
        WebApplicationException exception = new ForbiddenException("Access denied");

        Response response = mapper.toResponse(exception);

        assertEquals(403, response.getStatus());
        assertEquals("application/problem+json", response.getHeaders().getFirst("Content-Type"));

        ProblemDetails problemDetails = (ProblemDetails) response.getEntity();
        assertNotNull(problemDetails);
        assertEquals(403, problemDetails.getStatus());
        assertEquals("Forbidden", problemDetails.getTitle());
        assertEquals("Access denied", problemDetails.getDetail());
    }

    @Test
    void testMapGenericWebApplicationException() {
        WebApplicationException exception = new WebApplicationException("Server error", 500);

        Response response = mapper.toResponse(exception);

        assertEquals(500, response.getStatus());
        assertEquals("application/problem+json", response.getHeaders().getFirst("Content-Type"));

        ProblemDetails problemDetails = (ProblemDetails) response.getEntity();
        assertNotNull(problemDetails);
        assertEquals(500, problemDetails.getStatus());
        assertEquals("Internal Server Error", problemDetails.getTitle());
        assertEquals("Server error", problemDetails.getDetail());
    }

    @Test
    void testMapUnknownStatusCode() {
        WebApplicationException exception = new WebApplicationException("Unknown error", 418);

        Response response = mapper.toResponse(exception);

        assertEquals(418, response.getStatus());
        assertEquals("application/problem+json", response.getHeaders().getFirst("Content-Type"));

        ProblemDetails problemDetails = (ProblemDetails) response.getEntity();
        assertNotNull(problemDetails);
        assertEquals(418, problemDetails.getStatus());
        assertEquals("Error", problemDetails.getTitle());
        assertEquals("Unknown error", problemDetails.getDetail());
    }
}
