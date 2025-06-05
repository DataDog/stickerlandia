package com.datadoghq.stickerlandia.stickeraward.sticker;

import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetAllStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetStickerAssignmentsResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerDTO;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerImageUploadResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.UpdateStickerRequest;
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
import java.util.Date;
import org.eclipse.microprofile.openapi.annotations.Operation;

/** REST resource for managing stickers. */
@Path("/stickers")
public class StickerResource {

    @Inject StickerRepository stickerRepository;

    @Inject StickerImageService stickerImageService;

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
        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
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
        StickerDTO updated = stickerRepository.updateStickerMetadata(stickerId, data);
        if (updated == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
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
        try {
            boolean deleted = stickerRepository.deleteSticker(stickerId);
            if (!deleted) {
                return Response.status(Response.Status.NOT_FOUND).build();
            }
            return Response.noContent().build();
        } catch (IllegalStateException e) {
            return Response.status(Response.Status.BAD_REQUEST)
                    .entity("Cannot delete sticker that is assigned to users")
                    .build();
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
        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }

        if (metadata.getImageKey() == null) {
            return Response.status(Response.Status.NOT_FOUND)
                    .entity("No image found for this sticker")
                    .build();
        }

        try {
            InputStream imageStream = stickerImageService.getImage(metadata.getImageKey());
            return Response.ok(imageStream).type("image/png").build();
        } catch (Exception e) {
            return Response.status(Response.Status.INTERNAL_SERVER_ERROR)
                    .entity("Failed to retrieve image")
                    .build();
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
        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
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
            response.setUploadedAt(new Date());

            return Response.ok(response).build();
        } catch (Exception e) {
            System.out.println(e);
            return Response.status(Response.Status.INTERNAL_SERVER_ERROR)
                    .entity("Failed to upload image")
                    .build();
        }
    }

    /**
     * Gets all assignments for a specific sticker.
     *
     * @param stickerId the ID of the sticker
     * @param page the page number (0-based)
     * @param size the page size
     * @return response containing paginated assignments
     */
    @GET
    @Path("/{stickerId}/assignments")
    @Produces("application/json")
    @Operation(summary = "Get assignments for a sticker")
    public Response getStickerAssignments(
            @PathParam("stickerId") String stickerId,
            @QueryParam("page") @DefaultValue("0") int page,
            @QueryParam("size") @DefaultValue("20") int size) {
        GetStickerAssignmentsResponse data =
                stickerRepository.getStickerAssignments(stickerId, page, size);
        if (data == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }
        return Response.ok(data).build();
    }
}
