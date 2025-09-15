package com.datadoghq.stickerlandia.stickercatalogue;

import com.datadoghq.stickerlandia.common.dto.exception.ProblemDetailsResponseBuilder;
import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickercatalogue.dto.CreateStickerResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.GetAllStickersResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.StickerImageUploadResponse;
import com.datadoghq.stickerlandia.stickercatalogue.dto.UpdateStickerRequest;
import com.datadoghq.stickerlandia.stickercatalogue.mapper.StickerMapper;
import com.datadoghq.stickerlandia.stickercatalogue.result.DeleteResult;
import com.datadoghq.stickerlandia.stickercatalogue.result.ImageUploadResult;
import com.datadoghq.stickerlandia.stickercatalogue.result.StickerResult;
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
import org.eclipse.microprofile.openapi.annotations.Operation;
import org.jboss.logging.Logger;

/** REST resource for managing stickers. */
@Path("/api/stickers/v1")
public class StickerResource {

    @Inject StickerService stickerService;

    private static final Logger LOG = Logger.getLogger(StickerResource.class);

    /**
     * Gets all stickers with pagination.
     *
     * @param page the page number (0-based)
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

        return stickerService.getAllStickers(page, size);
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

        // Validate input
        if (data.getStickerName() == null || data.getStickerName().trim().isEmpty()) {
            return ProblemDetailsResponseBuilder.badRequest("Sticker name cannot be empty");
        }
        if (data.getStickerName().length() > 100) {
            return ProblemDetailsResponseBuilder.badRequest(
                    "Sticker name too long (max 100 characters)");
        }

        CreateStickerResponse createdSticker = stickerService.createSticker(data);
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

        StickerResult result = stickerService.getStickerById(stickerId);
        return switch (result) {
            case StickerResult.Success(var sticker) ->
                    Response.ok(StickerMapper.toDTO(sticker)).build();
            case StickerResult.NotFound(String id) ->
                    ProblemDetailsResponseBuilder.notFound("Sticker with ID " + id + " not found");
        };
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

        // Validate input
        if (data.getStickerName() != null) {
            if (data.getStickerName().trim().isEmpty()) {
                return ProblemDetailsResponseBuilder.badRequest("Sticker name cannot be empty");
            }
            if (data.getStickerName().length() > 100) {
                return ProblemDetailsResponseBuilder.badRequest(
                        "Sticker name too long (max 100 characters)");
            }
        }

        StickerResult result = stickerService.updateSticker(stickerId, data);
        return switch (result) {
            case StickerResult.Success(var sticker) ->
                    Response.ok(StickerMapper.toDTO(sticker)).build();
            case StickerResult.NotFound(String id) ->
                    ProblemDetailsResponseBuilder.notFound("Sticker with ID " + id + " not found");
        };
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

        DeleteResult result = stickerService.deleteSticker(stickerId);
        return switch (result) {
            case DeleteResult.Success() -> Response.noContent().build();
            case DeleteResult.NotFound(String id) ->
                    ProblemDetailsResponseBuilder.notFound("Sticker with ID " + id + " not found");
        };
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

        try {
            InputStream imageStream = stickerService.getStickerImageStream(stickerId);
            if (imageStream == null) {
                return ProblemDetailsResponseBuilder.notFound(
                        "No image found for sticker " + stickerId);
            }
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

        // Validate content type - only PNG accepted in this endpoint
        String contentType = "image/png";
        if (!isValidImageContentType(contentType)) {
            return ProblemDetailsResponseBuilder.badRequest(
                    "Only PNG and JPEG images are supported");
        }

        try {
            byte[] imageBytes = data.readAllBytes();
            InputStream imageStream = new java.io.ByteArrayInputStream(imageBytes);

            ImageUploadResult result =
                    stickerService.uploadStickerImage(
                            stickerId, imageStream, contentType, imageBytes.length);

            return switch (result) {
                case ImageUploadResult.Success(StickerImageUploadResponse response) ->
                        Response.ok(response).build();
                case ImageUploadResult.StickerNotFound(String id) ->
                        ProblemDetailsResponseBuilder.notFound(
                                "Sticker with ID " + id + " not found");
            };
        } catch (Exception e) {
            LOG.error("Failed to upload image", e);
            return ProblemDetailsResponseBuilder.internalServerError(
                    "Failed to upload image for sticker " + stickerId);
        }
    }

    private boolean isValidImageContentType(String contentType) {
        return contentType != null
                && (contentType.equals("image/png") || contentType.equals("image/jpeg"));
    }
}
