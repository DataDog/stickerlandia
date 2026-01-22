/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue;

import com.datadoghq.stickerlandia.stickercatalogue.entity.Sticker;
import com.datadoghq.stickerlandia.stickercatalogue.messaging.StickerEventPublisher;
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
                        "dd_oss.png"),
                new SampleSticker(
                        "sticker-004",
                        "AI Bits",
                        "Bits is ready for the AI revolution!",
                        -1,
                        "/stickers/ai-bits.png",
                        "Bits mascot wearing AI-powered visor with sparkles"),
                new SampleSticker(
                        "sticker-005",
                        "Serverless Bits",
                        "Functions as a service Bits.",
                        -1,
                        "/stickers/serverless-bits.jpeg",
                        "Bits mascot with serverless function tag"),
                new SampleSticker(
                        "sticker-006",
                        "Headphone Bits",
                        "Bits is in the zone, do not disturb!",
                        -1,
                        "/stickers/headphone-bits.png",
                        "Bits mascot wearing headphones"),
                new SampleSticker(
                        "sticker-007",
                        ".NET Bits",
                        "Bits loves building with .NET!",
                        -1,
                        "/stickers/dotnet-bits.png",
                        "Bits mascot with .NET logo"),
                new SampleSticker(
                        "sticker-008",
                        "PHP Bits",
                        "Bits is still rocking PHP!",
                        -1,
                        "/stickers/php-bits.png",
                        "Bits mascot with PHP logo"),
                new SampleSticker(
                        "sticker-009",
                        "C++ Bits",
                        "Bits gets close to the metal with C++.",
                        -1,
                        "/stickers/c___bits.png",
                        "Bits mascot with C++ logo"),
                new SampleSticker(
                        "sticker-010",
                        "Ruby Bits",
                        "Bits finds joy in Ruby development!",
                        -1,
                        "/stickers/ruby-bits.png",
                        "Bits mascot with Ruby gem logo"),
                new SampleSticker(
                        "sticker-011",
                        "Rust Bits",
                        "Bits guarantees memory safety!",
                        -1,
                        "/stickers/rust-bits.png",
                        "Bits mascot with Rust logo"),
                new SampleSticker(
                        "sticker-012",
                        "Java Bits",
                        "Bits writes once, runs anywhere!",
                        -1,
                        "/stickers/java-bits.png",
                        "Bits mascot with Java coffee cup logo"),
                new SampleSticker(
                        "sticker-013",
                        "Earth Bits",
                        "Bits cares about sustainability!",
                        -1,
                        "/stickers/earth-bits.png",
                        "Bits mascot with plant leaves"),
                new SampleSticker(
                        "sticker-014",
                        "Open Source Bits",
                        "Bits loves open source contributions!",
                        -1,
                        "/stickers/oss-bits.png",
                        "Bits mascot with open source logo"),
                new SampleSticker(
                        "sticker-015",
                        "Pride Bits",
                        "Bits celebrates Pride!",
                        -1,
                        "/stickers/pride-bits.png",
                        "Bits mascot with rainbow pride theme"),
                new SampleSticker(
                        "sticker-016",
                        "JavaScript Bits",
                        "Bits is building something awesome with JS!",
                        -1,
                        "/stickers/js-bits.png",
                        "Bits mascot with hard hat, wrench, and JavaScript book"),
                new SampleSticker(
                        "sticker-017",
                        "Holiday Bits",
                        "Bits wishes you happy holidays!",
                        -1,
                        "/stickers/holiday-bits.png",
                        "Bits mascot wearing scarf with holly"),
                new SampleSticker(
                        "sticker-018",
                        "Trace Bits",
                        "Bits follows the traces!",
                        -1,
                        "/stickers/trace-bits.png",
                        "Bits mascot with distributed tracing logo"),
                new SampleSticker(
                        "sticker-019",
                        "Incident Bits",
                        "Bits is on incident duty!",
                        -1,
                        "/stickers/incident-bits.png",
                        "Bits mascot as firefighter with hose"),
                new SampleSticker(
                        "sticker-020",
                        "Profiler Bits",
                        "Bits is optimizing performance!",
                        -1,
                        "/stickers/profiler-bits.png",
                        "Bits mascot holding a stopwatch"),
                new SampleSticker(
                        "sticker-021",
                        "Go Bits",
                        "Bits is ready to Go!",
                        -1,
                        "/stickers/go-bits.png",
                        "Bits mascot with Go language logo"),
                new SampleSticker(
                        "sticker-022",
                        "Kubernetes Bits",
                        "Bits is orchestrating containers!",
                        -1,
                        "/stickers/kubernetes-bits.png",
                        "Bits mascot with Kubernetes helm logo"),
                new SampleSticker(
                        "sticker-023",
                        "Python Bits",
                        "Bits loves Python!",
                        -1,
                        "/stickers/python-bits.png",
                        "Bits mascot with Python logo"),
                new SampleSticker(
                        "sticker-024",
                        "Hackerdog",
                        "Bits is in hacker mode!",
                        -1,
                        "/stickers/hackerdog.png",
                        "Bits mascot in black hoodie and sunglasses with cyberpunk background"),
                new SampleSticker(
                        "sticker-025",
                        "Winter Bits",
                        "Bits is ready for the slopes!",
                        -1,
                        "/stickers/winter-bits.png",
                        "Bits mascot wearing winter beanie with mountain scene"),
                new SampleSticker(
                        "sticker-026",
                        "Software Delivery Bits",
                        "Bits ships code with confidence!",
                        -1,
                        "/stickers/software-delivery-bits.png",
                        "Bits mascot in hoodie with laptop showing CI/CD logo"));
    }

    private record SampleSticker(
            String id,
            String name,
            String description,
            Integer quantityRemaining,
            String imagePath,
            String imageDescription) {}
}
