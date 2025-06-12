package com.datadoghq.stickerlandia.stickeraward.award;

import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.GetUserStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.RemoveStickerFromUserResponse;
import com.datadoghq.stickerlandia.stickeraward.award.messaging.StickerAwardEventPublisher;
import io.smallrye.common.constraint.NotNull;
import jakarta.inject.Inject;
import jakarta.transaction.Transactional;
import jakarta.ws.rs.Consumes;
import jakarta.ws.rs.DELETE;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;
import org.eclipse.microprofile.openapi.annotations.Operation;

/** REST resource for managing sticker awards and assignments. */
@Path("/api/award/v1/users")
public class StickerAwardResource {

    @Inject StickerAwardEventPublisher eventPublisher;

    @Inject StickerAwardRepository stickerAwardRepository;

    @Operation(
            description =
                    "Get stickers assigned to a user (access controlled based on caller identity)")
    @Path("/{userId}/stickers")
    @GET
    @Produces("application/json")
    public GetUserStickersResponse getUserStickers(@PathParam("userId") String userId) {
        return stickerAwardRepository.getUserStickers(userId);
    }

    /**
     * Assigns a sticker to a user.
     *
     * @param userId the ID of the user to assign the sticker to
     * @param request the assignment request containing sticker details
     * @return response containing assignment details
     */
    @POST
    @Path("/{userId}/stickers")
    @Operation(summary = "Assign a sticker to a user")
    @Consumes(MediaType.APPLICATION_JSON)
    @Produces(MediaType.APPLICATION_JSON)
    @Transactional
    public Response assignStickerToUser(
            @PathParam("userId") String userId, @NotNull AssignStickerRequest request) {

        try {
            String stickerId = request.getStickerId();
            AssignStickerResponse response =
                    stickerAwardRepository.assignStickerToUser(userId, stickerId, request);
            if (response == null) {
                return Response.status(Response.Status.BAD_REQUEST).build();
            }

            // Publish events - Note: We'll need to modify event publisher to work with DTOs
            // For now, skip event publishing until we refactor the event publisher
            // try {
            //     log.info("Publishing sticker assignment events for userId={}, stickerId={}",
            // userId, stickerId);
            //     eventPublisher.publishStickerAssigned(...);
            // } catch (Exception e) {
            //     log.error("Failed to publish sticker assignment events", e);
            // }

            return Response.status(Response.Status.CREATED).entity(response).build();
        } catch (IllegalStateException e) {
            return Response.status(Response.Status.CONFLICT).build();
        }
    }

    /**
     * Removes a sticker assignment from a user.
     *
     * @param userId the ID of the user
     * @param stickerId the ID of the sticker to remove
     * @return response indicating the removal result
     */
    @DELETE
    @Path("/{userId}/stickers/{stickerId}")
    @Operation(summary = "Remove a sticker assignment from a user")
    @Produces(MediaType.APPLICATION_JSON)
    @Transactional
    public Response removeStickerFromUser(
            @PathParam("userId") String userId, @PathParam("stickerId") String stickerId) {

        RemoveStickerFromUserResponse response =
                stickerAwardRepository.removeStickerFromUser(userId, stickerId);
        if (response == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }

        // Publish events - Note: We'll need to modify event publisher to work with DTOs
        // For now, skip event publishing until we refactor the event publisher
        // try {
        //     log.info("Publishing sticker removal event for userId={}, stickerId={}", userId,
        // stickerId);
        //     eventPublisher.publishStickerRemoved(...);
        // } catch (Exception e) {
        //     log.error("Failed to publish sticker removal event", e);
        // }

        return Response.ok(response).build();
    }
}
