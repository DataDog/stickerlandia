package com.datadoghq.stickerlandia.stickeraward.award;

import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.StickerAssignmentDTO;
import com.datadoghq.stickerlandia.stickeraward.award.dto.RemoveStickerFromUserResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.GetUserStickersResponse;
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
import jakarta.ws.rs.core.Response;
import org.eclipse.microprofile.openapi.annotations.Operation;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;


@Path("/api")
public class StickerAwardResource {
    
    private static final Logger log = LoggerFactory.getLogger(StickerAwardResource.class);
    
    @Inject
    StickerAwardEventPublisher eventPublisher;

    @Inject
    StickerAwardRepository stickerAwardRepository;

    @Operation(description = "Get stickers assigned to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @GET
    @Produces("application/json")
    public GetUserStickersResponse getUserStickers(@PathParam("userId") String userId) {
        return stickerAwardRepository.getUserStickers(userId);
    }

    @Operation(description = "Assign a new sticker to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @POST
    @Produces("application/json")
    @Consumes("application/json")
    @Transactional
    public Response assignStickerToUser(@PathParam("userId") String userId,
            @NotNull AssignStickerRequest data) {

        try {
            String stickerId = data.getStickerId();
            AssignStickerResponse response = stickerAwardRepository.assignStickerToUser(userId, stickerId, data);
            if (response == null) {
                return Response.status(Response.Status.BAD_REQUEST).build();
            }

            // Publish events - Note: We'll need to modify event publisher to work with DTOs
            // For now, skip event publishing until we refactor the event publisher
            // try {
            //     log.info("Publishing sticker assignment events for userId={}, stickerId={}", userId, stickerId);
            //     eventPublisher.publishStickerAssigned(...);
            // } catch (Exception e) {
            //     log.error("Failed to publish sticker assignment events", e);
            // }

            return Response.status(Response.Status.CREATED)
                .entity(response)
                .build();
        } catch (IllegalStateException e) {
            return Response.status(Response.Status.CONFLICT).build();
        }
    }

    @Operation(description = "Remove a sticker assignment from a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers/{stickerId}")
    @DELETE
    @Produces("application/json")
    @Transactional
    public Response removeStickerFromUser(@PathParam("userId") String userId,
            @PathParam("stickerId") String stickerId) {

        RemoveStickerFromUserResponse response = stickerAwardRepository.removeStickerFromUser(userId, stickerId);
        if (response == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }

        // Publish events - Note: We'll need to modify event publisher to work with DTOs
        // For now, skip event publishing until we refactor the event publisher
        // try {
        //     log.info("Publishing sticker removal event for userId={}, stickerId={}", userId, stickerId);
        //     eventPublisher.publishStickerRemoved(...);
        // } catch (Exception e) {
        //     log.error("Failed to publish sticker removal event", e);
        // }

        return Response.ok(response).build();
    }
}