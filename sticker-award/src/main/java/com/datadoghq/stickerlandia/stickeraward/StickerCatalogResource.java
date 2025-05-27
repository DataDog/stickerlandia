package com.datadoghq.stickerlandia.stickeraward;

import com.datadoghq.stickerlandia.stickeraward.beans.CreateStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerAssignmentsResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerCatalogResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerCreatedResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerMetadata;
import com.datadoghq.stickerlandia.stickeraward.beans.UpdateStickerCommand;
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

@Path("/api/award/v1/stickers")
public class StickerCatalogResource {

    @Inject
    StickerCatalogService stickerCatalogService;

    @GET
    @Produces("application/json")
    @Operation(description = "Get all stickers in the catalog")
    public StickerCatalogResponse getAllStickers(
            @QueryParam("page") @DefaultValue("0") int page,
            @QueryParam("size") @DefaultValue("20") int size) {
        return stickerCatalogService.getAllStickers(page, size);
    }

    @POST
    @Produces("application/json")
    @Consumes("application/json")
    @Operation(description = "Create a new sticker in the catalog")
    public Response createSticker(@NotNull CreateStickerCommand data) {
        StickerCreatedResponse createdSticker = stickerCatalogService.createSticker(data);
        return Response.status(Response.Status.CREATED)
            .entity(createdSticker)
            .build();
    }

    @GET
    @Path("/{stickerId}")
    @Produces("application/json")
    @Operation(description = "Get a specific sticker's metadata")
    public Response getStickerMetadata(@PathParam("stickerId") String stickerId) {
        StickerMetadata metadata = stickerCatalogService.getStickerMetadata(stickerId);
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
            @NotNull UpdateStickerCommand data) {
        StickerMetadata updated = stickerCatalogService.updateStickerMetadata(stickerId, data);
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
            boolean deleted = stickerCatalogService.deleteSticker(stickerId);
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
        return Response.status(Response.Status.NOT_IMPLEMENTED)
            .entity("Image storage not implemented yet")
            .build();
    }

    @PUT
    @Path("/{stickerId}/image")
    @Produces("application/json")
    @Consumes("image/png")
    @Operation(description = "Upload or update the sticker image")
    public Response uploadStickerImage(
            @PathParam("stickerId") String stickerId,
            @NotNull InputStream data) {
        return Response.status(Response.Status.NOT_IMPLEMENTED)
            .entity("Image upload not implemented yet")
            .build();
    }

    @GET
    @Path("/{stickerId}/assignments")
    @Produces("application/json")
    @Operation(description = "Get users to which this sticker is assigned")
    public Response getStickerAssignments(
            @PathParam("stickerId") String stickerId,
            @QueryParam("page") @DefaultValue("0") int page,
            @QueryParam("size") @DefaultValue("20") int size) {
        StickerAssignmentsResponse data = stickerCatalogService.getStickerAssignments(stickerId, page, size);
        if (data == null) {
            return Response.status(Response.Status.NOT_FOUND).build();
        }
        return Response.ok(data).build();
    }
}