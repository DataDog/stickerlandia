package com.datadoghq.stickerlandia.stickeraward;

import com.datadoghq.stickerlandia.stickeraward.beans.AssignStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerAssignmentResponseApiResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerRemovalResponseApiResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.UserStickersResponseApiResponse;
import io.smallrye.common.constraint.NotNull;
import jakarta.ws.rs.Consumes;
import jakarta.ws.rs.DELETE;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import org.eclipse.microprofile.openapi.annotations.Operation;

/**
 * A JAX-RS interface. An implementation of this interface must be provided.
 */
@Path("/api")
public interface ApiResource {
  /**
   * <p>
   * Get stickers assigned to a user (access controlled based on caller identity)
   * </p>
   * 
   */
  @Operation(description = "Get stickers assigned to a user (access controlled based on caller identity)")
  @Path("/award/v1/users/{userId}/stickers")
  @GET
  @Produces("application/json")
  UserStickersResponseApiResponse generatedMethod1(@PathParam("userId") String userId);

  /**
   * <p>
   * Assign a new sticker to a user (access controlled based on caller identity)
   * </p>
   * 
   */
  @Operation(description = "Assign a new sticker to a user (access controlled based on caller identity)")
  @Path("/award/v1/users/{userId}/stickers")
  @POST
  @Produces("application/json")
  @Consumes("application/json")
  StickerAssignmentResponseApiResponse generatedMethod2(@PathParam("userId") String userId,
      @NotNull AssignStickerCommand data);

  /**
   * <p>
   * Remove a sticker assignment from a user (access controlled based on caller
   * identity)
   * </p>
   * 
   */
  @Operation(description = "Remove a sticker assignment from a user (access controlled based on caller identity)")
  @Path("/award/v1/users/{userId}/stickers/{stickerId}")
  @DELETE
  @Produces("application/json")
  StickerRemovalResponseApiResponse generatedMethod3(@PathParam("userId") String userId,
      @PathParam("stickerId") String stickerId);
}
