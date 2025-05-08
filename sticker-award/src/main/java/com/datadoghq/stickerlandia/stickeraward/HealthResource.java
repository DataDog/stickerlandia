package com.datadoghq.stickerlandia.stickeraward;

import jakarta.ws.rs.GET;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;
import org.eclipse.microprofile.openapi.annotations.Operation;

import java.util.HashMap;
import java.util.Map;

@Path("/health")
public class HealthResource {
    
    /**
     * Check service health
     */
    @Operation(description = "Check service health")
    @GET
    @Produces(MediaType.APPLICATION_JSON)
    public Response checkHealth() {
        Map<String, Object> healthStatus = new HashMap<>();
        healthStatus.put("status", "OK");
        return Response.ok(healthStatus).build();
    }
}