package com.datadoghq.stickerlandia.common.health;

import jakarta.ws.rs.GET;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;
import java.util.HashMap;
import java.util.Map;
import org.eclipse.microprofile.openapi.annotations.Operation;

/** Health check resource for the sticker award service. */
@Path("/health")
public class HealthResource {

    /**
     * Basic health check endpoint.
     *
     * @return health status response
     */
    @GET
    @Produces(MediaType.APPLICATION_JSON)
    @Operation(summary = "Health check endpoint")
    public Response health() {
        Map<String, Object> healthStatus = new HashMap<>();
        healthStatus.put("status", "OK");
        return Response.ok(healthStatus).build();

    }
}
