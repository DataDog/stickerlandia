package com.datadoghq.stickerlandia.stickeraward;

import com.datadoghq.stickerlandia.stickeraward.beans.AssignStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerAssignmentResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerAssignmentResponseApiResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerDTO;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerRemovalResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerRemovalResponseApiResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.UserStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.UserStickersResponseApiResponse;
import com.datadoghq.stickerlandia.stickeraward.entity.Sticker;
import com.datadoghq.stickerlandia.stickeraward.entity.StickerAssignment;
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

import java.time.Instant;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.stream.Collectors;

@Path("/api")
public class StickerAwardResource {

    @Operation(description = "Get stickers assigned to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @GET
    @Produces("application/json")
    @Transactional
    public UserStickersResponseApiResponse getUserStickers(@PathParam("userId") String userId) {
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

        UserStickersResponseApiResponse apiResponse = new UserStickersResponseApiResponse();
        apiResponse.setSuccess(true);
        apiResponse.setMessage("Successfully retrieved user stickers");
        apiResponse.setData(response);

        return apiResponse;
    }

    @Operation(description = "Assign a new sticker to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @POST
    @Produces("application/json")
    @Consumes("application/json")
    @Transactional
    public StickerAssignmentResponseApiResponse assignStickerToUser(@PathParam("userId") String userId,
            @NotNull AssignStickerCommand data) {

        String stickerId = data.getStickerId();
        Sticker sticker = Sticker.findById(stickerId);

        if (sticker == null) {
            StickerAssignmentResponseApiResponse apiResponse = new StickerAssignmentResponseApiResponse();
            apiResponse.setSuccess(false);
            apiResponse.setMessage("Sticker not found: " + stickerId);
            return apiResponse;
        }

        // Check if sticker is already assigned to the user and not removed
        StickerAssignment existingAssignment = StickerAssignment.findActiveByUserAndSticker(userId, stickerId);
        if (existingAssignment != null) {
            StickerAssignmentResponseApiResponse apiResponse = new StickerAssignmentResponseApiResponse();
            apiResponse.setSuccess(false);
            apiResponse.setMessage("User already has this sticker assigned");
            return apiResponse;
        }

        // Create new assignment
        StickerAssignment assignment = new StickerAssignment(userId, sticker, data.getReason());
        assignment.persist();

        // Create response
        StickerAssignmentResponse response = new StickerAssignmentResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setAssignedAt(Date.from(assignment.getAssignedAt()));

        StickerAssignmentResponseApiResponse apiResponse = new StickerAssignmentResponseApiResponse();
        apiResponse.setSuccess(true);
        apiResponse.setMessage("Sticker assigned successfully");
        apiResponse.setData(response);

        return apiResponse;
    }

    @Operation(description = "Remove a sticker assignment from a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers/{stickerId}")
    @DELETE
    @Produces("application/json")
    @Transactional
    public StickerRemovalResponseApiResponse removeStickerFromUser(@PathParam("userId") String userId,
            @PathParam("stickerId") String stickerId) {

        StickerAssignment assignment = StickerAssignment.findActiveByUserAndSticker(userId, stickerId);

        if (assignment == null) {
            StickerRemovalResponseApiResponse apiResponse = new StickerRemovalResponseApiResponse();
            apiResponse.setSuccess(false);
            apiResponse.setMessage("Active sticker assignment not found for user");
            return apiResponse;
        }

        // Mark as removed
        assignment.setRemovedAt(Instant.now());
        assignment.persist();

        // Create response
        StickerRemovalResponse response = new StickerRemovalResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setRemovedAt(Date.from(assignment.getRemovedAt()));

        StickerRemovalResponseApiResponse apiResponse = new StickerRemovalResponseApiResponse();
        apiResponse.setSuccess(true);
        apiResponse.setMessage("Sticker removed successfully");
        apiResponse.setData(response);

        return apiResponse;
    }
}