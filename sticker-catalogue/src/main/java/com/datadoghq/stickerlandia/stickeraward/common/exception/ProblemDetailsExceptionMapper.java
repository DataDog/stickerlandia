package com.datadoghq.stickerlandia.stickeraward.common.exception;

import com.datadoghq.stickerlandia.stickeraward.common.dto.ProblemDetails;
import jakarta.ws.rs.WebApplicationException;
import jakarta.ws.rs.core.Response;
import jakarta.ws.rs.ext.ExceptionMapper;
import jakarta.ws.rs.ext.Provider;

/** JAX-RS exception mapper for WebApplicationException to RFC 7807 ProblemDetails. */
@Provider
public class ProblemDetailsExceptionMapper implements ExceptionMapper<WebApplicationException> {

    private static final String PROBLEM_JSON_TYPE = "application/problem+json";

    @Override
    public Response toResponse(WebApplicationException exception) {
        int status = exception.getResponse().getStatus();
        String title = getDefaultTitle(status);
        String detail = exception.getMessage();

        ProblemDetails problemDetails = new ProblemDetails(status, title, detail);

        return Response.status(status)
                .entity(problemDetails)
                .header("Content-Type", PROBLEM_JSON_TYPE)
                .build();
    }

    private String getDefaultTitle(int status) {
        return switch (status) {
            case 400 -> "Bad Request";
            case 401 -> "Unauthorized";
            case 403 -> "Forbidden";
            case 404 -> "Not Found";
            case 409 -> "Conflict";
            case 500 -> "Internal Server Error";
            default -> "Error";
        };
    }
}
