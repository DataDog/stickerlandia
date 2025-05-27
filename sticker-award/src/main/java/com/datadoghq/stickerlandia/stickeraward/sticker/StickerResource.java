package com.datadoghq.stickerlandia.stickeraward.sticker;

import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerRequest;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetStickerAssignmentsResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.GetAllStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.CreateStickerResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerDTO;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerImageUploadResponse;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.StickerMetadata;
import com.datadoghq.stickerlandia.stickeraward.sticker.dto.UpdateStickerRequest;
import io.smallrye.common.constraint.NotNull;
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
import org.eclipse.microprofile.openapi.annotations.Operation;
import jakarta.inject.Inject;

import java.io.InputStream;
import java.util.Date;

@Path("/api/award/v1/stickers")
public class StickerResource {

    @Inject
    StickerRepository stickerRepository;

    @Inject
    StickerImageService stickerImageService;

    @GET
    @Produces("application/json")
    @Operation(description = "Get all stickers in the catalog")
    public GetAllStickersResponse getAllStickers(
            @QueryParam("page") @DefaultValue("0") int page,
            @QueryParam("size") @DefaultValue("20") int size) {
        return stickerRepository.getAllStickers(page, size);
    }

    @POST
    @Produces("application/json")
    @Consumes("application/json")
    @Operation(description = "Create a new sticker in the catalog")
    public Response createSticker(@NotNull CreateStickerRequest data) {
        CreateStickerResponse createdSticker = stickerRepository.createSticker(data);
        return Response.status(Response.Status.CREATED)
            .entity(createdSticker)
            .build();
    }

    @GET
    @Path("/{stickerId}")
    @Produces("application/json")
    @Operation(description = "Get a specific sticker's metadata")
    public Response getStickerMetadata(@PathParam("stickerId") String stickerId) {
        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }
        return Response.ok(metadata).build();
    }

    @PUT
    @Path("/{stickerId}")
    @Produces("application/json")
    @Consumes("application/json")
    @Operation(description = "Update a sticker's metadata")
    public Response updateStickerMetadata(
            @PathParam("stickerId") String stickerId,
            @NotNull UpdateStickerRequest data) {
        StickerDTO updated = stickerRepository.updateStickerMetadata(stickerId, data);
        if (updated == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }
        return Response.ok(updated).build();
    }

    @DELETE
    @Path("/{stickerId}")
    @Operation(description = "Delete a sticker from the catalog. A sticker can only be deleted if it is not assigned to anyone.")
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

    @GET
    @Path("/{stickerId}/image")
    @Produces("image/png")
    @Operation(description = "Get the sticker image")
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
            return Response.ok(imageStream)
                .type("image/png")
                .build();
        } catch (Exception e) {
            return Response.status(Response.Status.INTERNAL_SERVER_ERROR)
                .entity("Failed to retrieve image")
                .build();
        }
    }

    @PUT
    @Path("/{stickerId}/image")
    @Produces("application/json")
    @Consumes("image/png")
    @Operation(description = "Upload or update the sticker image")
    public Response uploadStickerImage(
            @PathParam("stickerId") String stickerId,
            @NotNull InputStream data) {
        StickerDTO metadata = stickerRepository.getStickerMetadata(stickerId);
        if (metadata == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }
        
        try {
            byte[] imageBytes = data.readAllBytes();
            String imageKey = stickerImageService.uploadImage(new java.io.ByteArrayInputStream(imageBytes), "image/png", imageBytes.length);
            
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

    @GET
    @Path("/{stickerId}/assignments")
    @Produces("application/json")
    @Operation(description = "Get users to which this sticker is assigned")
    public Response getStickerAssignments(
            @PathParam("stickerId") String stickerId,
            @QueryParam("page") @DefaultValue("0") int page,
            @QueryParam("size") @DefaultValue("20") int size) {
        GetStickerAssignmentsResponse data = stickerRepository.getStickerAssignments(stickerId, page, size);
        if (data == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }
        return Response.ok(data).build();
    }
}
