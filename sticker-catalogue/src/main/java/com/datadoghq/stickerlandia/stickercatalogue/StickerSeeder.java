/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue;

import com.datadoghq.stickerlandia.stickercatalogue.entity.Sticker;
import io.opentelemetry.api.trace.Span;
import io.opentelemetry.api.trace.Tracer;
import io.opentelemetry.context.Context;
import io.opentelemetry.context.Scope;
import io.quarkus.runtime.StartupEvent;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.event.Observes;
import jakarta.inject.Inject;
import jakarta.transaction.Transactional;
import java.io.InputStream;
import java.util.List;
import org.jboss.logging.Logger;

@ApplicationScoped
public class StickerSeeder {

    private static final Logger LOG = Logger.getLogger(StickerSeeder.class);

    @Inject StickerEventPublisher eventPublisher;

    @Inject StickerImageService stickerImageService;

    @Inject StickerRepository stickerRepository;

    @Inject Tracer tracer;

    @Transactional
    public void onStartup(@Observes StartupEvent ev) {

        // Tracer tracer = GlobalOpenTelemetry.getTracer("startup");

        Span rootSpan = tracer.spanBuilder("SeedStickers").setNoParent().startSpan();

        try (Scope scope = rootSpan.makeCurrent()) {
            LOG.info("Starting sticker seeding...");

            seedStickers(tracer);

            LOG.info("Sticker seeding completed successfully");

        } catch (Exception e) {
            rootSpan.recordException(e);
            rootSpan.setStatus(io.opentelemetry.api.trace.StatusCode.ERROR, "Critical failure");
            LOG.error("Failed to seed stickers", e);
            throw new RuntimeException("Critical failure during sticker seeding", e);
        } finally {
            rootSpan.end();
        }
    }

    private void seedStickers(Tracer tracer) {
        List<SampleSticker> sampleStickers = getSampleStickers();
        int created = 0;
        int skipped = 0;

        for (SampleSticker sample : sampleStickers) {
            Span childSpan =
                    tracer.spanBuilder("SeedSticker")
                            .setParent(Context.current())
                            .setAttribute("sticker.id", sample.id)
                            .setAttribute("sticker.name", sample.name)
                            .setAttribute("sticker.image_path", sample.imagePath)
                            .startSpan();

            try (Scope childScope = childSpan.makeCurrent()) {
                if (seedSticker(tracer, sample)) {
                    created++;
                } else {
                    skipped++;
                }
            } catch (Exception e) {
                childSpan.recordException(e);
                childSpan.setStatus(
                        io.opentelemetry.api.trace.StatusCode.ERROR, "Failed seeding sticker");
                throw e;
            } finally {
                childSpan.end();
            }
        }

        LOG.infof(
                "Sticker seeding completed: %d created, %d skipped, %d total",
                created, skipped, sampleStickers.size());
    }

    private boolean seedSticker(Tracer tracer, SampleSticker sample) {
        Sticker existingSticker = stickerRepository.findById(sample.id);
        if (existingSticker != null) {
            LOG.debugf("Sticker %s already exists, checking image", sample.id);

            // Seed image if sticker exists but doesn't have an image
            if (sample.imagePath != null) {
                seedStickerImage(tracer, sample.id, sample.imagePath, sample.imageDescription);
            }

            return false;
        }

        Sticker sticker =
                new Sticker(sample.id, sample.name, sample.description, sample.quantityRemaining);

        sticker.persist();

        eventPublisher.publishStickerAdded(
                sticker.getStickerId(), sticker.getName(), sticker.getDescription());

        LOG.infof("Created sample sticker: %s (%s)", sample.id, sample.name);

        // Seed image if available
        if (sample.imagePath != null) {
            seedStickerImage(tracer, sample.id, sample.imagePath, sample.imageDescription);
        }

        return true;
    }

    private void seedStickerImage(
            Tracer tracer, String stickerId, String resourcePath, String description) {
        Span imageSpan =
                tracer.spanBuilder("SeedStickerImage")
                        .setParent(Context.current())
                        .setAttribute("sticker.id", stickerId)
                        .setAttribute("resource.path", resourcePath)
                        .startSpan();

        try (Scope scope = imageSpan.makeCurrent()) {
            // Check if sticker exists and doesn't already have an image
            Sticker sticker = stickerRepository.findById(stickerId);
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
            imageSpan.recordException(e);
            imageSpan.setStatus(
                    io.opentelemetry.api.trace.StatusCode.ERROR, "Failed seeding sticker image");
            LOG.errorf(e, "Failed to seed image for sticker %s from %s", stickerId, resourcePath);
        } finally {
            imageSpan.end();
        }
    }

    private List<SampleSticker> getSampleStickers() {
        return List.of(
                new SampleSticker(
                        "sticker-001",
                        "Welcome!",
                        "Welcome to Stickerlandia! Enjoy your stay.",
                        -1,
                        "/stickers/dd_icon_rgb.png",
                        "Datadog Purple Logo"),
                new SampleSticker(
                        "sticker-002",
                        "DASH 2025 Attendee",
                        "Thanks for attending DASH!",
                        2000,
                        "/stickers/dd_icon_white.png",
                        "Datadog White Logo"),
                new SampleSticker(
                        "sticker-003",
                        "Datadog OSS Contributor",
                        "Thanks for contributing to our opensource code!",
                        -1,
                        null,
                        "dd_oss.png"));
    }

    private record SampleSticker(
            String id,
            String name,
            String description,
            Integer quantityRemaining,
            String imagePath,
            String imageDescription) {}
}
