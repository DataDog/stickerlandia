package com.datadoghq.stickerlandia.stickeraward.sticker;

import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.common.dto.PagedResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetStickerAssignmentsResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetAllStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerDTO;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.UpdateStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.award.dto.UserAssignmentDTO;
import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;
import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;
import io.quarkus.hibernate.orm.panache.PanacheQuery;
import io.quarkus.panache.common.Page;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.transaction.Transactional;

import java.time.Instant;
import java.util.Date;
import java.util.List;
import java.util.UUID;
import java.util.stream.Collectors;

@ApplicationScoped
public class StickerRepository {

    @Transactional
    public CreateStickerResponse createSticker(CreateStickerRequest request) {
        String stickerId = "sticker-" + UUID.randomUUID().toString().substring(0, 8);
        
        Sticker sticker = new Sticker(
            stickerId,
            request.getStickerName(),
            request.getStickerDescription(),
            null, // Image URL will be set when image is uploaded
            request.getStickerQuantityRemaining()
        );
        
        sticker.persist();
        
        CreateStickerResponse response = new CreateStickerResponse();
        response.setStickerId(sticker.getStickerId());
        response.setStickerName(sticker.getName());
        response.setImageUrl(buildImageUrl(sticker.getStickerId()));
        return response;
    }

    public GetAllStickersResponse getAllStickers(int page, int size) {
        PanacheQuery<Sticker> query = Sticker.findAll();
        List<Sticker> stickers = query.page(Page.of(page, size)).list();
        long totalCount = query.count();
        
        List<StickerDTO> stickerDTOList = stickers.stream()
            .map(this::toStickerMetadata)
            .collect(Collectors.toList());
        
        PagedResponse pagination = new PagedResponse();
        pagination.setPage(page);
        pagination.setSize(size);
        pagination.setTotal((int) totalCount);
        pagination.setTotalPages((int) Math.ceil((double) totalCount / size));
        
        GetAllStickersResponse response = new GetAllStickersResponse();
        response.setStickers(stickerDTOList);
        response.setPagination(pagination);
        return response;
    }

    public StickerDTO getStickerMetadata(String stickerId) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }
        return toStickerMetadata(sticker);
    }

    @Transactional
    public StickerDTO updateStickerMetadata(String stickerId, UpdateStickerRequest request) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }
        
        if (request.getStickerName() != null) {
            sticker.setName(request.getStickerName());
        }
        if (request.getStickerDescription() != null) {
            sticker.setDescription(request.getStickerDescription());
        }
        if (request.getStickerQuantityRemaining() != null) {
            sticker.setStickerQuantityRemaining(request.getStickerQuantityRemaining());
        }
        
        sticker.setUpdatedAt(Instant.now());
        sticker.persist();
        
        return toStickerMetadata(sticker);
    }

    @Transactional
    public boolean deleteSticker(String stickerId) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return false;
        }
        
        // Check if sticker is assigned to any users
        long assignmentCount = StickerAssignment.countActiveByStickerId(stickerId);
        if (assignmentCount > 0) {
            throw new IllegalStateException("Cannot delete sticker that is assigned to users");
        }
        
        sticker.delete();
        return true;
    }

    public GetStickerAssignmentsResponse getStickerAssignments(String stickerId, int page, int size) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }
        
        PanacheQuery<StickerAssignment> query = StickerAssignment.find("sticker.stickerId = ?1 AND removedAt IS NULL", stickerId);
        List<StickerAssignment> assignments = query.page(Page.of(page, size)).list();
        long totalCount = query.count();
        
        List<UserAssignmentDTO> userAssignments = assignments.stream()
            .map(assignment -> {
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

    private StickerDTO toStickerMetadata(Sticker sticker) {
        StickerDTO metadata = new StickerDTO();
        metadata.setStickerId(sticker.getStickerId());
        metadata.setStickerName(sticker.getName());
        metadata.setStickerDescription(sticker.getDescription());
        metadata.setStickerQuantityRemaining(sticker.getStickerQuantityRemaining());
        metadata.setImageUrl(buildImageUrl(sticker.getStickerId()));
        metadata.setCreatedAt(Date.from(sticker.getCreatedAt()));
        metadata.setUpdatedAt(sticker.getUpdatedAt() != null ? Date.from(sticker.getUpdatedAt()) : null);
        return metadata;
    }

    private String buildImageUrl(String stickerId) {
        return "/api/award/v1/stickers/" + stickerId + "/image";
    }
}