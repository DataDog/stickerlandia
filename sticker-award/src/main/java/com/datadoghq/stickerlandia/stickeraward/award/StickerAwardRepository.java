package com.datadoghq.stickerlandia.stickeraward.award;

import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.award.dto.AssignStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.GetUserStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.RemoveStickerFromUserResponse;
import com.datadoghq.stickerlandia.stickeraward.award.dto.StickerAssignmentDTO;
import com.datadoghq.stickerlandia.stickeraward.award.dto.UserAssignmentDTO;
import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import com.datadoghq.stickerlandia.stickeraward.common.dto.PagedResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetStickerAssignmentsResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;
import io.quarkus.hibernate.orm.panache.PanacheQuery;
import io.quarkus.panache.common.Page;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.transaction.Transactional;
import java.time.Instant;
import java.util.Date;
import java.util.List;
import java.util.stream.Collectors;

/** Repository class for managing sticker award operations. */
@ApplicationScoped
public class StickerAwardRepository {

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
                                    Sticker sticker = assignment.getSticker();
                                    StickerAssignmentDTO dto = new StickerAssignmentDTO();
                                    dto.setStickerId(sticker.getStickerId());
                                    dto.setName(sticker.getName());
                                    dto.setDescription(sticker.getDescription());
                                    dto.setImageUrl(sticker.getImageUrl());
                                    dto.setAssignedAt(Date.from(assignment.getAssignedAt()));
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
        // Find the sticker to assign
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null; // Sticker not found
        }

        // Check if user already has this sticker
        StickerAssignment existingAssignment =
                StickerAssignment.findActiveByUserAndSticker(userId, stickerId);
        if (existingAssignment != null) {
            throw new IllegalStateException("User already has this sticker assigned");
        }

        // Create new assignment
        StickerAssignment assignment = new StickerAssignment();
        assignment.setUserId(userId);
        assignment.setSticker(sticker);
        assignment.setReason(request.getReason());
        assignment.setAssignedAt(Instant.now());
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
     * Gets assignments for a specific sticker.
     *
     * @param stickerId the ID of the sticker
     * @param page the page number (0-based)
     * @param size the page size
     * @return response containing paginated assignments
     */
    public GetStickerAssignmentsResponse getStickerAssignments(
            String stickerId, int page, int size) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }

        PanacheQuery<StickerAssignment> query =
                StickerAssignment.find("sticker.stickerId = ?1 AND removedAt IS NULL", stickerId);
        List<StickerAssignment> assignments = query.page(Page.of(page, size)).list();
        long totalCount = query.count();

        final List<UserAssignmentDTO> userAssignments =
                assignments.stream()
                        .map(
                                assignment -> {
                                    UserAssignmentDTO ua = new UserAssignmentDTO();
                                    ua.setUserId(assignment.getUserId());
                                    ua.setAssignedAt(Date.from(assignment.getAssignedAt()));
                                    ua.setReason(assignment.getReason());
                                    return ua;
                                })
                        .collect(Collectors.toList());

        PagedResponse pagination = new PagedResponse();
        pagination.setPage(page);
        pagination.setSize(size);
        pagination.setTotal((int) totalCount);
        pagination.setTotalPages((int) Math.ceil((double) totalCount / size));

        GetStickerAssignmentsResponse response = new GetStickerAssignmentsResponse();
        response.setStickerId(stickerId);
        response.setAssignments(userAssignments);
        response.setPagination(pagination);
        return response;
    }
}
