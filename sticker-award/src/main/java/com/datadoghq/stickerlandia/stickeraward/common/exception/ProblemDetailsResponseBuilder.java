package com.datadoghq.stickerlandia.stickeraward.common.exception;

import com.datadoghq.stickerlandia.stickeraward.common.dto.ProblemDetails;
import jakarta.ws.rs.core.Response;

/** Utility class for building RFC 7807 ProblemDetails HTTP responses. */
public class ProblemDetailsResponseBuilder {

    private static final String PROBLEM_JSON_TYPE = "application/problem+json";

    public static Response badRequest(String detail) {
        return buildResponse(400, "Bad Request", detail);
    }

    public static Response unauthorized(String detail) {
        return buildResponse(401, "Unauthorized", detail);
    }

    public static Response forbidden(String detail) {
        return buildResponse(403, "Forbidden", detail);
    }

    public static Response notFound(String detail) {
        return buildResponse(404, "Not Found", detail);
    }

    public static Response conflict(String detail) {
        return buildResponse(409, "Conflict", detail);
    }

    public static Response internalServerError(String detail) {
        return buildResponse(500, "Internal Server Error", detail);
    }

    private static Response buildResponse(int status, String title, String detail) {
        ProblemDetails problemDetails = new ProblemDetails(status, title, detail);

        return Response.status(status)
                .entity(problemDetails)
                .header("Content-Type", PROBLEM_JSON_TYPE)
                .build();
    }
}
