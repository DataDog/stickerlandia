package com.datadoghq.stickerlandia.stickeraward;

import com.datadoghq.stickerlandia.stickeraward.beans.AssignStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerAssignmentResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerDTO;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerRemovalResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.UserStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.entity.Sticker;
import com.datadoghq.stickerlandia.stickeraward.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.messaging.StickerEventPublisher;
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

import java.time.Instant;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.stream.Collectors;

@Path("/api")
public class StickerAwardResource {
    
    private static final Logger log = LoggerFactory.getLogger(StickerAwardResource.class);
    
    @Inject
    StickerEventPublisher eventPublisher;

    @Operation(description = "Get stickers assigned to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @GET
    @Produces("application/json")
    @Transactional
    public UserStickersResponse getUserStickers(@PathParam("userId") String userId) {
        List<StickerAssignment> assignments = StickerAssignment.findActiveByUserId(userId);

        List<StickerDTO> stickerDTOs = assignments.stream()
                .map(assignment -> {
                    Sticker sticker = assignment.getSticker();
                    StickerDTO dto = new StickerDTO();
                    dto.setStickerId(sticker.getStickerId());
                    dto.setName(sticker.getName());
                    dto.setDescription(sticker.getDescription());
                    dto.setImageUrl(sticker.getImageUrl());
                    dto.setAssignedAt(Date.from(assignment.getAssignedAt()));
                    return dto;
                })
                .collect(Collectors.toList());

        UserStickersResponse response = new UserStickersResponse();
        response.setUserId(userId);
        response.setStickers(stickerDTOs);

        return response;
    }

    @Operation(description = "Assign a new sticker to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @POST
    @Produces("application/json")
    @Consumes("application/json")
    @Transactional
    public Response assignStickerToUser(@PathParam("userId") String userId,
            @NotNull AssignStickerCommand data) {

        String stickerId = data.getStickerId();
        Sticker sticker = Sticker.findById(stickerId);

        if (sticker == null) {
            return Response.status(Response.Status.BAD_REQUEST).build();
        }

        // Check if sticker is already assigned to the user and not removed
        StickerAssignment existingAssignment = StickerAssignment.findActiveByUserAndSticker(userId, stickerId);
        if (existingAssignment != null) {
            return Response.status(Response.Status.CONFLICT).build();
        }

        // Create new assignment
        StickerAssignment assignment = new StickerAssignment(userId, sticker, data.getReason());
        assignment.persist();
        
        // Publish events to Kafka
        try {
            log.info("Publishing sticker assignment events for userId={}, stickerId={}", userId, stickerId);
            eventPublisher.publishStickerAssigned(assignment);
        } catch (Exception e) {
            log.error("Failed to publish sticker assignment events", e);
            // Continue with the response even if the event publishing fails
            // In a production system, you might want to implement a retry mechanism
        }

        // Create response
        StickerAssignmentResponse response = new StickerAssignmentResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setAssignedAt(Date.from(assignment.getAssignedAt()));

        return Response.status(Response.Status.CREATED)
            .entity(response)
            .build();
    }

    @Operation(description = "Remove a sticker assignment from a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers/{stickerId}")
    @DELETE
    @Produces("application/json")
    @Transactional
    public Response removeStickerFromUser(@PathParam("userId") String userId,
            @PathParam("stickerId") String stickerId) {

        StickerAssignment assignment = StickerAssignment.findActiveByUserAndSticker(userId, stickerId);

        if (assignment == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }

        // Mark as removed
        assignment.setRemovedAt(Instant.now());
        assignment.persist();
        
        // Publish events to Kafka
        try {
            log.info("Publishing sticker removal event for userId={}, stickerId={}", userId, stickerId);
            eventPublisher.publishStickerRemoved(assignment);
        } catch (Exception e) {
            log.error("Failed to publish sticker removal event", e);
            // Continue with the response even if the event publishing fails
            // In a production system, you might want to implement a retry mechanism
        }

        // Create response
        StickerRemovalResponse response = new StickerRemovalResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setRemovedAt(Date.from(assignment.getRemovedAt()));

        return Response.ok(response).build();
    }
}