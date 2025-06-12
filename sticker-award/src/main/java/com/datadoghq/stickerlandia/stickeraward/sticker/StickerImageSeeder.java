package com.datadoghq.stickerlandia.stickeraward.sticker;

import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;
import io.quarkus.runtime.StartupEvent;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.event.Observes;
import jakarta.inject.Inject;
import jakarta.transaction.Transactional;
import java.io.InputStream;
import org.jboss.logging.Logger;

/**
 * Service responsible for seeding sticker images during application startup. This class loads
 * default sticker images from the classpath and uploads them to the configured storage service,
 * associating them with existing stickers.
 */
@ApplicationScoped
public class StickerImageSeeder {

    private static final Logger LOG = Logger.getLogger(StickerImageSeeder.class);

    @Inject StickerImageService stickerImageService;

    @Inject StickerRepository stickerRepository;

    /**
     * Handles the startup event to seed sticker images. This method is called during application
     * startup and ensures that default sticker images are loaded and associated with existing
     * stickers.
     *
     * @param ev the startup event
     */
    @Transactional
    public void onStartup(@Observes StartupEvent ev) {
        LOG.info("Starting sticker image seeding...");

        try {
            seedStickerImage("sticker-001", "/stickers/dd_icon_rgb.png", "Datadog Purple Logo");
            seedStickerImage("sticker-002", "/stickers/dd_icon_white.png", "Datadog White Logo");

            LOG.info("Sticker image seeding completed successfully");
        } catch (Exception e) {
            LOG.error("Failed to seed sticker images", e);
        }
    }

    private void seedStickerImage(String stickerId, String resourcePath, String description) {
        // Check if sticker exists and doesn't already have an image
        Sticker sticker = Sticker.findById(stickerId);
        if (sticker == null) {
            LOG.warnf("Sticker %s not found, skipping image seeding", stickerId);
            return;
        }

        if (sticker.getImageKey() != null && !sticker.getImageKey().isEmpty()) {
            LOG.infof(
                    "Sticker %s already has image key %s, skipping",
                    stickerId, sticker.getImageKey());
            return;
        }

        try {
            // Load image from resources
            InputStream imageStream = getClass().getResourceAsStream(resourcePath);
            if (imageStream == null) {
                LOG.errorf("Could not find image resource: %s", resourcePath);
                return;
            }

            // Get file size for upload
            byte[] imageData = imageStream.readAllBytes();
            imageStream.close();

            // Upload image to our storage service
            InputStream uploadStream = getClass().getResourceAsStream(resourcePath);
            String imageKey =
                    stickerImageService.uploadImage(uploadStream, "image/png", imageData.length);
            uploadStream.close();

            // Update sticker with image key
            stickerRepository.updateStickerImageKey(stickerId, imageKey);

            LOG.infof(
                    "Successfully seeded image for sticker %s with key %s (%s)",
                    stickerId, imageKey, description);

        } catch (Exception e) {
            LOG.errorf(e, "Failed to seed image for sticker %s from %s", stickerId, resourcePath);
        }
    }
}
