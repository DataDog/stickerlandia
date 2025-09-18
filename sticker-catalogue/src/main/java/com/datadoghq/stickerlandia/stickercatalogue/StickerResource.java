/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue;

import com.datadoghq.stickerlandia.common.dto.exception.ProblemDetailsResponseBuilder;
import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.GetAllStickersResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.StickerDTO;
import com.datadoghq.stickerlandia.stickercatalogue.dto.StickerImageUploadResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.UpdateStickerRequest;
import io.opentelemetry.api.trace.Span;
import io.smallrye.common.constraint.NotNull;
import jakarta.inject.Inject;
import jakarta.ws.rs.Consumes;
import jakarta.ws.rs.DELETE;
import jakarta.ws.rs.DefaultValue;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.PUT;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.QueryParam;
import jakarta.ws.rs.core.Response;
import java.io.InputStream;
import java.time.Instant;
import org.eclipse.microprofile.openapi.annotations.Operation;
import org.jboss.logging.Logger;

/** REST resource for managing stickers. */
@Path("/api/stickers/v1")
public class StickerResource {

    @Inject StickerRepository stickerRepository;

    @Inject StickerImageService stickerImageService;

    private static final Logger LOG = Logger.getLogger(StickerResource.class);

    /**
     * Gets all stickers with pagination.
     *
     * @param size the page size
     * @return response containing paginated stickers
     */
    @GET
    @Produces("application/json")
    @Operation(summary = "Get all stickers")
    public GetAllStickersResponse getAllStickers(
            @QueryParam("page") @DefaultValue("0") int page,
            @QueryParam("size") @DefaultValue("20") int size) {

        LOG.info("GetAllStickers");

        return stickerRepository.getAllStickers(page, size);
    }

    /**
     * Creates a new sticker.
     *
     * @param data the sticker creation request
     * @return response containing the created sticker details
     */
    @POST
    @Produces("application/json")
    @Consumes("application/json")
    @Operation(summary = "Create a new sticker")
    public Response createSticker(@NotNull CreateStickerRequest data) {
        LOG.info("Create sticker");
        Span span = Span.current();
        span.setAttribute("sticker.name", data.getStickerName());

        CreateStickerResponse createdSticker = stickerRepository.createSticker(data);
        return Response.status(Response.Status.CREATED).entity(createdSticker).build();
    }

    /**
     * Gets a specific sticker by ID.
     *
     * @param stickerId the ID of the sticker
     * @return response containing the sticker details
     */
    @GET
    @Path("/{stickerId}")
    @Produces("application/json")
    @Operation(summary = "Get a sticker by ID")
    public Response getStickerMetadata(@PathParam("stickerId") String stickerId) {
        LOG.info("GetSticker");
        Span span = Span.current();
        span.setAttribute("sticker.id", stickerId);

        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return ProblemDetailsResponseBuilder.notFound(
                    "Sticker with ID " + stickerId + " not found");
        }
        return Response.ok(metadata).build();
    }

    /**
     * Updates an existing sticker.
     *
     * @param stickerId the ID of the sticker to update
     * @param data the update request
     * @return response containing the updated sticker details
     */
    @PUT
    @Path("/{stickerId}")
    @Produces("application/json")
    @Consumes("application/json")
    @Operation(summary = "Update a sticker")
    public Response updateStickerMetadata(
            @PathParam("stickerId") String stickerId, @NotNull UpdateStickerRequest data) {

        LOG.info("Update sticker with: " + data.toString());
        Span span = Span.current();
        span.setAttribute("sticker.id", stickerId);

        StickerDTO updated = stickerRepository.updateStickerMetadata(stickerId, data);
        if (updated == null) {
            return ProblemDetailsResponseBuilder.notFound(
                    "Sticker with ID " + stickerId + " not found");
        }
        return Response.ok(updated).build();
    }

    /**
     * Deletes a sticker from the catalog.
     *
     * @param stickerId the ID of the sticker to delete
     * @return response indicating the deletion result
     */
    @DELETE
    @Path("/{stickerId}")
    @Operation(summary = "Delete a sticker from the catalog")
    public Response deleteSticker(@PathParam("stickerId") String stickerId) {
        LOG.info("Delete sticker");
        Span span = Span.current();
        span.setAttribute("sticker.id", stickerId);

        try {
            boolean deleted = stickerRepository.deleteSticker(stickerId);
            if (!deleted) {
                return ProblemDetailsResponseBuilder.notFound(
                        "Sticker with ID " + stickerId + " not found");
            }
            return Response.noContent().build();
        } catch (IllegalStateException e) {
            return ProblemDetailsResponseBuilder.badRequest(
                    "Cannot delete sticker that is assigned to users");
        }
    }

    /**
     * Gets the image for a specific sticker.
     *
     * @param stickerId the ID of the sticker
     * @return response containing the sticker image
     */
    @GET
    @Path("/{stickerId}/image")
    @Produces("image/png")
    @Operation(summary = "Get the sticker image")
    public Response getStickerImage(@PathParam("stickerId") String stickerId) {

        LOG.info("Get Sticker Image");
        Span span = Span.current();
        span.setAttribute("sticker.id", stickerId);

        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return ProblemDetailsResponseBuilder.notFound(
                    "Sticker with ID " + stickerId + " not found");
        }

        if (metadata.getImageKey() == null) {
            return ProblemDetailsResponseBuilder.notFound(
                    "No image found for sticker " + stickerId);
        }

        try {
            InputStream imageStream = stickerImageService.getImage(metadata.getImageKey());
            return Response.ok(imageStream).type("image/png").build();
        } catch (Exception e) {
            return ProblemDetailsResponseBuilder.internalServerError(
                    "Failed to retrieve image for sticker " + stickerId);
        }
    }

    /**
     * Uploads an image for a sticker.
     *
     * @param stickerId the ID of the sticker
     * @param data the image input stream
     * @return response containing the upload result
     */
    @POST
    @Path("/{stickerId}/image")
    @Consumes("image/png")
    @Produces("application/json")
    @Operation(summary = "Upload an image for a sticker")
    public Response uploadStickerImage(
            @PathParam("stickerId") String stickerId, @NotNull InputStream data) {

        LOG.info("Upload image for sticker");
        Span span = Span.current();
        span.setAttribute("sticker.id", stickerId);

        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return ProblemDetailsResponseBuilder.notFound(
                    "Sticker with ID " + stickerId + " not found");
        }

        try {
            byte[] imageBytes = data.readAllBytes();
            String imageKey =
                    stickerImageService.uploadImage(
                            new java.io.ByteArrayInputStream(imageBytes),
                            "image/png",
                            imageBytes.length);

            stickerRepository.updateStickerImageKey(stickerId, imageKey);

            String imageUrl = stickerImageService.getImageUrl(imageKey);

            StickerImageUploadResponse response = new StickerImageUploadResponse();
            response.setStickerId(stickerId);
            response.setImageUrl(imageUrl);
            response.setUploadedAt(Instant.now());

            return Response.ok(response).build();
        } catch (Exception e) {
            System.out.println(e);
            return ProblemDetailsResponseBuilder.internalServerError(
                    "Failed to upload image for sticker " + stickerId);
        }
    }
}
