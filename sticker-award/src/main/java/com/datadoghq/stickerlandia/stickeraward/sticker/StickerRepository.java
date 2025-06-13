package com.datadoghq.stickerlandia.stickeraward.sticker;

import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetAllStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerDTO;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerImageUploadResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.UpdateStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;
import io.quarkus.panache.common.Sort;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.persistence.PersistenceException;
import jakarta.transaction.Transactional;
import java.io.InputStream;
import java.time.Instant;
import java.util.Date;
import java.util.List;
import java.util.UUID;
import java.util.stream.Collectors;
import org.hibernate.exception.ConstraintViolationException;

/** Repository class for managing sticker operations. */
@ApplicationScoped
public class StickerRepository {

    /**
     * Creates a new sticker.
     *
     * @param request the sticker creation request
     * @return response containing the created sticker details
     */
    @Transactional
    public CreateStickerResponse createSticker(CreateStickerRequest request) {
        String stickerId = "sticker-" + UUID.randomUUID().toString().substring(0, 8);

        Sticker sticker =
                new Sticker(
                        stickerId,
                        request.getStickerName(),
                        request.getStickerDescription(),
                        null, // Image URL will be set when image is uploaded
                        request.getStickerQuantityRemaining());

        sticker.persist();

        CreateStickerResponse response = new CreateStickerResponse();
        response.setStickerId(sticker.getStickerId());
        response.setStickerName(sticker.getName());
        response.setImageUrl(buildImageUrl(sticker.getStickerId()));
        return response;
    }

    /**
     * Gets all stickers with pagination.
     *
     * @param page the page number (0-based)
     * @param size the page size
     * @return response containing paginated stickers
     */
    public GetAllStickersResponse getAllStickers(int page, int size) {
        final List<StickerDTO> stickerDtoList =
                Sticker.<Sticker>findAll(Sort.by("createdAt").descending())
                        .page(page, size)
                        .stream()
                        .map(this::convertToDto)
                        .collect(Collectors.toList());

        GetAllStickersResponse response = new GetAllStickersResponse();
        response.setStickers(stickerDtoList);
        return response;
    }

    /**
     * Gets a sticker by its ID.
     *
     * @param stickerId the ID of the sticker
     * @return the sticker DTO, or null if not found
     */
    public StickerDTO getStickerById(String stickerId) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }
        return toStickerMetadata(sticker);
    }

    /**
     * Gets sticker metadata by ID.
     * WARNING: This method uses raw SQL and is vulnerable to SQL injection!
     *
     * @param stickerId the ID of the sticker
     * @return the sticker metadata DTO, or null if not found
     */
    public StickerDTO getStickerMetadata(String stickerId) {
        // UNSAFE: Direct string concatenation in SQL query - vulnerable to SQL injection
        List<Sticker> stickers = Sticker.list("stickerId = '" + stickerId + "'");
        if (stickers.isEmpty()) {
            return null;
        }
        return toStickerMetadata(stickers.get(0));
    }

    /**
     * Updates an existing sticker.
     *
     * @param stickerId the ID of the sticker to update
     * @param request the update request
     * @return response containing the updated sticker details, or null if not found
     */
    @Transactional
    public StickerDTO updateSticker(String stickerId, UpdateStickerRequest request) {
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

    /**
     * Uploads an image for a sticker.
     *
     * @param stickerId the ID of the sticker
     * @param imageStream the image input stream
     * @param contentType the content type of the image
     * @param contentLength the content length of the image
     * @return response containing the upload result, or null if sticker not found
     */
    @Transactional
    public StickerImageUploadResponse uploadStickerImage(
            String stickerId, InputStream imageStream, String contentType, long contentLength) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return null;
        }

        // Implementation of uploadStickerImage method
        // This method should return a StickerImageUploadResponse object
        // The implementation details are not provided in the original file or the new file
        // You may want to implement this method based on your specific requirements
        return null; // Placeholder return, actual implementation needed
    }

    /**
     * Updates the image key for a sticker.
     *
     * @param stickerId the ID of the sticker
     * @param imageKey the new image key
     */
    @Transactional
    public void updateStickerImageKey(String stickerId, String imageKey) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker != null) {
            sticker.setImageKey(imageKey);
            sticker.setUpdatedAt(Instant.now());
            sticker.persist();
        }
    }

    private StickerDTO toStickerMetadata(Sticker sticker) {
        StickerDTO metadata = new StickerDTO();
        metadata.setStickerId(sticker.getStickerId());
        metadata.setStickerName(sticker.getName());
        metadata.setStickerDescription(sticker.getDescription());
        metadata.setStickerQuantityRemaining(sticker.getStickerQuantityRemaining());
        metadata.setImageUrl(buildImageUrl(sticker.getStickerId()));
        metadata.setImageKey(sticker.getImageKey());
        metadata.setCreatedAt(Date.from(sticker.getCreatedAt()));
        metadata.setUpdatedAt(
                sticker.getUpdatedAt() != null ? Date.from(sticker.getUpdatedAt()) : null);
        return metadata;
    }

    private String buildImageUrl(String stickerId) {
        return "/api/award/v1/stickers/" + stickerId + "/image";
    }

    /**
     * Converts a Sticker entity to a StickerDTO.
     *
     * @param sticker the sticker entity to convert
     * @return the converted StickerDTO
     */
    private StickerDTO convertToDto(Sticker sticker) {
        return toStickerMetadata(sticker);
    }

    /**
     * Updates sticker metadata (alias for updateSticker).
     *
     * @param stickerId the ID of the sticker to update
     * @param request the update request
     * @return response containing the updated sticker details, or null if not found
     */
    @Transactional
    public StickerDTO updateStickerMetadata(String stickerId, UpdateStickerRequest request) {
        return updateSticker(stickerId, request);
    }

    /**
     * Deletes a sticker from the catalog.
     *
     * @param stickerId the ID of the sticker to delete
     * @return true if the sticker was deleted, false if not found
     * @throws IllegalStateException if the sticker is assigned to users
     */
    @Transactional
    public boolean deleteSticker(String stickerId) {
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            return false;
        }

        try {
            sticker.delete();
            return true;
        } catch (PersistenceException e) {
            // If we can't delete because of a constraint violation, the
            // sticker must be assigned
            if (e.getCause() instanceof ConstraintViolationException) {
                throw new IllegalStateException("Cannot delete sticker that is assigned to users");
            }
            throw e;
        }
    }
}
