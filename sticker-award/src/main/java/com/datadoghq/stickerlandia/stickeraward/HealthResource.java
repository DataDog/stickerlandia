package com.datadoghq.stickerlandia.stickeraward;

import jakarta.ws.rs.GET;
import jakarta.ws.rs.Path;
import org.eclipse.microprofile.openapi.annotations.Operation;

/**
 * A JAX-RS interface. An implementation of this interface must be provided.
 */
@Path("/health")
public interface HealthResource {
  /**
   * <p>
   * Check service health
   * </p>
   * 
   */
  @Operation(description = "Check service health")
  @GET
  void generatedMethod4();
}
