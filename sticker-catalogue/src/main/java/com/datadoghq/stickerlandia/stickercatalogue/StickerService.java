package com.datadoghq.stickerlandia.stickercatalogue;

import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.GetAllStickersResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.StickerImageUploadResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.UpdateStickerRequest;
import com.datadoghq.stickerlandia.stickercatalogue.entity.Sticker;
import com.datadoghq.stickerlandia.stickercatalogue.result.DeleteResult;
import com.datadoghq.stickerlandia.stickercatalogue.result.ImageUploadResult;
import com.datadoghq.stickerlandia.stickercatalogue.result.StickerResult;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.inject.Inject;
import jakarta.transaction.Transactional;
import java.io.InputStream;
import java.time.Instant;
import org.jboss.logging.Logger;

/**
 * Service layer for sticker business logic.
 */
@ApplicationScoped
public class StickerService {

    @Inject StickerRepository stickerRepository;

    @Inject StickerImageStore stickerImageStore;

    @Inject StickerEventPublisher eventPublisher;

    private static final Logger LOG = Logger.getLogger(StickerService.class);

    /** Gets all stickers with pagination. */
    public GetAllStickersResponse getAllStickers(int page, int size) {
        LOG.infof("Getting stickers - page: %d, size: %d", page, size);
        return stickerRepository.getAllStickers(page, size);
    }

    /** Gets a sticker by ID. */
    public StickerResult getStickerById(String id) {
        LOG.infof("Getting sticker by ID: %s", id);
        Sticker sticker = Sticker.findByStickerId(id);
        if (sticker == null) {
            return new StickerResult.NotFound(id);
        }
        return new StickerResult.Success(sticker);
    }

    /** Creates a new sticker. */
    @Transactional
    public CreateStickerResponse createSticker(CreateStickerRequest request) {
        LOG.infof("Creating sticker: %s", request.getStickerName());

        // Delegate to repository
        CreateStickerResponse response = stickerRepository.createSticker(request);

        // Publish sticker added event
        eventPublisher.publishStickerAdded(
                response.getStickerId(), response.getStickerName(), request.getStickerDescription());

        return response;
    }

    /** Updates an existing sticker. */
    @Transactional
    public StickerResult updateSticker(String id, UpdateStickerRequest request) {
        LOG.infof("Updating sticker ID: %s", id);

        // Check if sticker exists
        StickerResult existingSticker = getStickerById(id);
        if (existingSticker instanceof StickerResult.NotFound) {
            return existingSticker;
        }

        Sticker sticker = ((StickerResult.Success) existingSticker).sticker();

        // Update fields if provided
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

        // Publish update event
        eventPublisher.publishStickerUpdated(
                sticker.getStickerId(), sticker.getName(), sticker.getDescription());

        return new StickerResult.Success(sticker);
    }

    /** Deletes a sticker by ID. */
    @Transactional
    public DeleteResult deleteSticker(String id) {
        LOG.infof("Deleting sticker ID: %s", id);

        // Check if sticker exists
        StickerResult existingSticker = getStickerById(id);
        if (existingSticker instanceof StickerResult.NotFound) {
            return new DeleteResult.NotFound(id);
        }

        Sticker sticker = ((StickerResult.Success) existingSticker).sticker();

        // Publish delete event before deletion
        eventPublisher.publishStickerDeleted(sticker.getStickerId(), sticker.getName());

        sticker.delete();
        return new DeleteResult.Success();
    }

    /** Uploads an image for a sticker. */
    @Transactional
    public ImageUploadResult uploadStickerImage(
            String stickerId, InputStream imageStream, String contentType, int contentLength) {
        LOG.infof("Uploading image for sticker ID: %s", stickerId);

        // Check if sticker exists
        StickerResult existingSticker = getStickerById(stickerId);
        if (existingSticker instanceof StickerResult.NotFound) {
            return new ImageUploadResult.StickerNotFound(stickerId);
        }

        Sticker sticker = ((StickerResult.Success) existingSticker).sticker();

        // Process image through image service
        String imageKey = stickerImageStore.uploadImage(imageStream, contentType, contentLength);

        // Update sticker with new image
        sticker.setImageKey(imageKey);
        sticker.setUpdatedAt(Instant.now());
        sticker.persist();

        // Get image URL
        String imageUrl = stickerImageStore.getImageUrl(imageKey);

        StickerImageUploadResponse response = new StickerImageUploadResponse();
        response.setStickerId(stickerId);
        response.setImageUrl(imageUrl);
        response.setUploadedAt(Instant.now());

        return new ImageUploadResult.Success(response);
    }

    /** Gets image stream for a sticker. */
    public InputStream getStickerImageStream(String stickerId) {
        LOG.infof("Getting image for sticker ID: %s", stickerId);

        StickerResult stickerResult = getStickerById(stickerId);
        if (stickerResult instanceof StickerResult.NotFound) {
            return null;
        }

        Sticker sticker = ((StickerResult.Success) stickerResult).sticker();
        if (sticker.getImageKey() == null) {
            return null;
        }

        return stickerImageStore.getImage(sticker.getImageKey());
    }
}
