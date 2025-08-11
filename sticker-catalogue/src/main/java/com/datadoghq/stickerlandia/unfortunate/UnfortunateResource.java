package com.datadoghq.stickerlandia.unfortunate;

import com.datadoghq.stickerlandia.common.dto.exception.ProblemDetailsResponseBuilder;
import com.datadoghq.stickerlandia.unfortunate.dto.CreateUnfortunateRequest;
import com.datadoghq.stickerlandia.unfortunate.dto.CreateUnfortunateResponse;
import com.datadoghq.stickerlandia.unfortunate.dto.GetAllUnfortunatesResponse;
import com.datadoghq.stickerlandia.unfortunate.dto.UnfortunateDTO;
import io.smallrye.common.constraint.NotNull;
import jakarta.inject.Inject;
import jakarta.ws.rs.Consumes;
import jakarta.ws.rs.DELETE;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.Response;
import org.eclipse.microprofile.openapi.annotations.Operation;

@Path("/api/unfortunates/v1")
public class UnfortunateResource {

    @Inject UnfortunateRepository unfortunateRepository;

    @GET
    @Produces("application/json")
    @Operation(summary = "Get all unfortunate events")
    public GetAllUnfortunatesResponse getAllUnfortunates() {
        return unfortunateRepository.getAllUnfortunates();
    }

    @POST
    @Produces("application/json")
    @Consumes("application/json")
    @Operation(summary = "Create a new unfortunate event")
    public Response createUnfortunate(@NotNull CreateUnfortunateRequest data) {
        CreateUnfortunateResponse createdUnfortunate = unfortunateRepository.createUnfortunate(data);
        return Response.status(Response.Status.CREATED).entity(createdUnfortunate).build();
    }

    @GET
    @Path("/{id}")
    @Produces("application/json")
    @Operation(summary = "Get an unfortunate event by ID")
    public Response getUnfortunateById(@PathParam("id") String id) {
        UnfortunateDTO unfortunate = unfortunateRepository.getUnfortunateById(id);
        if (unfortunate == null) {
            return ProblemDetailsResponseBuilder.notFound(
                    "Unfortunate event with ID " + id + " not found");
        }
        return Response.ok(unfortunate).build();
    }

    @DELETE
    @Path("/{id}")
    @Operation(summary = "Delete an unfortunate event")
    public Response deleteUnfortunate(@PathParam("id") String id) {
        boolean deleted = unfortunateRepository.deleteUnfortunate(id);
        if (!deleted) {
            return ProblemDetailsResponseBuilder.notFound(
                    "Unfortunate event with ID " + id + " not found");
        }
        return Response.noContent().build();
    }
}