package com.datadoghq.stickerlandia.stickeraward.sticker;

import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.common.dto.PagedResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerAssignmentsResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerCatalogResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerCreatedResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerMetadata;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.UpdateStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.award.dto.UserAssignment;
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
    public StickerCreatedResponse createSticker(CreateStickerCommand command) {
        String stickerId = "sticker-" + UUID.randomUUID().toString().substring(0, 8);
        
        Sticker sticker = new Sticker(
            stickerId,
            command.getStickerName(),
            command.getStickerDescription(),
            null, // Image URL will be set when image is uploaded
            command.getStickerQuantityRemaining()
        );
        
        sticker.persist();
        
        StickerCreatedResponse response = new StickerCreatedResponse();
        response.setStickerId(sticker.getStickerId());
        response.setStickerName(sticker.getName());
        response.setImageUrl(buildImageUrl(sticker.getStickerId()));
        return response;
    }

    public StickerCatalogResponse getAllStickers(int page, int size) {
        PanacheQuery<Sticker> query = Sticker.findAll();
        List<Sticker> stickers = query.page(Page.of(page, size)).list();
        long totalCount = query.count();
        
        List<StickerMetadata> stickerMetadataList = stickers.stream()
            .map(this::toStickerMetadata)
            .collect(Collectors.toList());
        
        PagedResponse pagination = new PagedResponse();
        pagination.setPage(page);
        pagination.setSize(size);
        pagination.setTotal((int) totalCount);
        pagination.setTotalPages((int) Math.ceil((double) totalCount / size));
        
        StickerCatalogResponse response = new StickerCatalogResponse();
        response.setStickers(stickerMetadataList);
        response.setPagination(pagination);
        return response;
    }

    public StickerMetadata getStickerMetadata(String stickerId) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }
        return toStickerMetadata(sticker);
    }

    @Transactional
    public StickerMetadata updateStickerMetadata(String stickerId, UpdateStickerCommand command) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }
        
        if (command.getStickerName() != null) {
            sticker.setName(command.getStickerName());
        }
        if (command.getStickerDescription() != null) {
            sticker.setDescription(command.getStickerDescription());
        }
        if (command.getStickerQuantityRemaining() != null) {
            sticker.setStickerQuantityRemaining(command.getStickerQuantityRemaining());
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

    public StickerAssignmentsResponse getStickerAssignments(String stickerId, int page, int size) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }
        
        PanacheQuery<StickerAssignment> query = StickerAssignment.find("sticker.stickerId = ?1 AND removedAt IS NULL", stickerId);
        List<StickerAssignment> assignments = query.page(Page.of(page, size)).list();
        long totalCount = query.count();
        
        List<UserAssignment> userAssignments = assignments.stream()
            .map(assignment -> {
                UserAssignment ua = new UserAssignment();
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
        
        StickerAssignmentsResponse response = new StickerAssignmentsResponse();
        response.setStickerId(stickerId);
        response.setAssignments(userAssignments);
        response.setPagination(pagination);
        return response;
    }

    private StickerMetadata toStickerMetadata(Sticker sticker) {
        StickerMetadata metadata = new StickerMetadata();
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