package com.datadoghq.stickerlandia.stickeraward.award;

import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.GetUserStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.RemoveStickerFromUserResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.StickerAssignmentDTO;
import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.inject.Inject;
import jakarta.persistence.EntityManager;
import jakarta.transaction.Transactional;
import java.time.Instant;
import java.util.Date;
import java.util.List;
import java.util.stream.Collectors;

/** Repository class for managing sticker award operations. */
@ApplicationScoped
public class StickerAwardRepository {

    @Inject EntityManager entityManager;

    /**
     * Gets all sticker assignments for a specific user.
     *
     * @param userId the ID of the user
     * @return response containing user's sticker assignments
     */
    public GetUserStickersResponse getUserStickers(String userId) {
        List<StickerAssignment> assignments = StickerAssignment.findActiveByUserId(userId);

        List<StickerAssignmentDTO> stickerAssignmentDtos =
                assignments.stream()
                        .map(
                                assignment -> {
                                    StickerAssignmentDTO dto = new StickerAssignmentDTO();
                                    dto.setStickerId(assignment.getStickerId());
                                    dto.setAssignedAt(assignment.getAssignedAt());
                                    dto.setReason(assignment.getReason());
                                    return dto;
                                })
                        .collect(Collectors.toList());

        GetUserStickersResponse response = new GetUserStickersResponse();
        response.setUserId(userId);
        response.setStickers(stickerAssignmentDtos);

        return response;
    }

    /**
     * Assigns a sticker to a user.
     *
     * @param userId the ID of the user
     * @param stickerId the ID of the sticker
     * @param request the assignment request
     * @return response containing assignment details, or null if assignment failed
     */
    @Transactional
    public AssignStickerResponse assignStickerToUser(
            String userId, String stickerId, AssignStickerRequest request) {
        // Check if sticker exists
        if (!stickerExists(stickerId)) {
            return null; // Sticker not found
        }

        // Check if user already has this sticker
        StickerAssignment existingAssignment =
                StickerAssignment.findActiveByUserAndSticker(userId, stickerId);
        if (existingAssignment != null) {
            throw new IllegalStateException("User already has this sticker assigned");
        }

        // Create new assignment using sticker ID only
        StickerAssignment assignment =
                new StickerAssignment(userId, stickerId, request.getReason());
        assignment.persist();

        // Create response
        AssignStickerResponse response = new AssignStickerResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setAssignedAt(Date.from(assignment.getAssignedAt()));

        return response;
    }

    /**
     * Removes a sticker assignment from a user.
     *
     * @param userId the ID of the user
     * @param stickerId the ID of the sticker to remove
     * @return response containing removal details, or null if removal failed
     */
    @Transactional
    public RemoveStickerFromUserResponse removeStickerFromUser(String userId, String stickerId) {
        // Find the active assignment
        StickerAssignment assignment =
                StickerAssignment.findActiveByUserAndSticker(userId, stickerId);
        if (assignment == null) {
            return null; // Assignment not found
        }

        // Mark as removed
        assignment.setRemovedAt(Instant.now());
        assignment.persist();

        // Create response
        RemoveStickerFromUserResponse response = new RemoveStickerFromUserResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setRemovedAt(Date.from(assignment.getRemovedAt()));

        return response;
    }

    /**
     * Checks if a sticker exists without importing the Sticker entity. Uses a native query to avoid
     * cross-domain violations.
     *
     * @param stickerId the ID of the sticker to check
     * @return true if the sticker exists, false otherwise
     */
    private boolean stickerExists(String stickerId) {
        Object result =
                entityManager
                        .createNativeQuery("SELECT COUNT(*) FROM stickers WHERE sticker_id = ?")
                        .setParameter(1, stickerId)
                        .getSingleResult();
        return ((Number) result).longValue() > 0;
    }
}
