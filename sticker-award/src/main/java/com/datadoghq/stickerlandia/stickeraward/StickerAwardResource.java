package com.datadoghq.stickerlandia.stickeraward;

import com.datadoghq.stickerlandia.stickeraward.beans.AssignStickerCommand;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerAssignmentResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerAssignmentResponseApiResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerDTO;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerRemovalResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.StickerRemovalResponseApiResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.UserStickersResponse;
import com.datadoghq.stickerlandia.stickeraward.beans.UserStickersResponseApiResponse;
import io.smallrye.common.constraint.NotNull;
import jakarta.ws.rs.Consumes;
import jakarta.ws.rs.DELETE;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.Response;
import org.eclipse.microprofile.openapi.annotations.Operation;

import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

@Path("/api")
public class StickerAwardResource {
    
    // Sample in-memory storage for demonstration
    private final Map<String, List<StickerDTO>> userStickers = new HashMap<>();
    
    // Some sample stickers
    private final Map<String, StickerDTO> availableStickers = new HashMap<>();
    
    public StickerAwardResource() {
        // Initialize some sample stickers
        StickerDTO debuggingHero = new StickerDTO();
        debuggingHero.setStickerId("sticker-001");
        debuggingHero.setName("Debugging Hero");
        debuggingHero.setDescription("Awarded for exceptional debugging skills");
        debuggingHero.setImageUrl("https://stickerlandia.example.com/images/debugging-hero.png");
        
        StickerDTO codeReviewChampion = new StickerDTO();
        codeReviewChampion.setStickerId("sticker-002");
        codeReviewChampion.setName("Code Review Champion");
        codeReviewChampion.setDescription("Awarded for thorough code reviews");
        codeReviewChampion.setImageUrl("https://stickerlandia.example.com/images/code-review-champion.png");
        
        StickerDTO performanceOptimizer = new StickerDTO();
        performanceOptimizer.setStickerId("sticker-003");
        performanceOptimizer.setName("Performance Optimizer");
        performanceOptimizer.setDescription("Awarded for significant performance improvements");
        performanceOptimizer.setImageUrl("https://stickerlandia.example.com/images/performance-optimizer.png");
        
        availableStickers.put(debuggingHero.getStickerId(), debuggingHero);
        availableStickers.put(codeReviewChampion.getStickerId(), codeReviewChampion);
        availableStickers.put(performanceOptimizer.getStickerId(), performanceOptimizer);
        
        // Sample user with a sticker
        String sampleUserId = "user-001";
        List<StickerDTO> stickersList = new ArrayList<>();
        
        StickerDTO userSticker = new StickerDTO();
        userSticker.setStickerId(debuggingHero.getStickerId());
        userSticker.setName(debuggingHero.getName());
        userSticker.setDescription(debuggingHero.getDescription());
        userSticker.setImageUrl(debuggingHero.getImageUrl());
        userSticker.setAssignedAt(new Date());
        
        stickersList.add(userSticker);
        userStickers.put(sampleUserId, stickersList);
    }

    @Operation(description = "Get stickers assigned to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @GET
    @Produces("application/json")
    public UserStickersResponseApiResponse getUserStickers(@PathParam("userId") String userId) {
        UserStickersResponse response = new UserStickersResponse();
        response.setUserId(userId);
        
        List<StickerDTO> stickers = userStickers.getOrDefault(userId, new ArrayList<>());
        response.setStickers(stickers);
        
        UserStickersResponseApiResponse apiResponse = new UserStickersResponseApiResponse();
        apiResponse.setSuccess(true);
        apiResponse.setMessage("Successfully retrieved user stickers");
        apiResponse.setData(response);
        
        return apiResponse;
    }

    @Operation(description = "Assign a new sticker to a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers")
    @POST
    @Produces("application/json")
    @Consumes("application/json")
    public StickerAssignmentResponseApiResponse assignStickerToUser(@PathParam("userId") String userId,
            @NotNull AssignStickerCommand data) {
        
        String stickerId = data.getStickerId();
        StickerDTO stickerToAssign = availableStickers.get(stickerId);
        
        if (stickerToAssign == null) {
            StickerAssignmentResponseApiResponse apiResponse = new StickerAssignmentResponseApiResponse();
            apiResponse.setSuccess(false);
            apiResponse.setMessage("Sticker not found: " + stickerId);
            return apiResponse;
        }
        
        // Create new sticker instance with assignment time
        StickerDTO userSticker = new StickerDTO();
        userSticker.setStickerId(stickerToAssign.getStickerId());
        userSticker.setName(stickerToAssign.getName());
        userSticker.setDescription(stickerToAssign.getDescription());
        userSticker.setImageUrl(stickerToAssign.getImageUrl());
        userSticker.setAssignedAt(new Date());
        
        // Add to user's stickers
        List<StickerDTO> stickers = userStickers.computeIfAbsent(userId, k -> new ArrayList<>());
        stickers.add(userSticker);
        
        // Create response
        StickerAssignmentResponse response = new StickerAssignmentResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setAssignedAt(userSticker.getAssignedAt());
        
        StickerAssignmentResponseApiResponse apiResponse = new StickerAssignmentResponseApiResponse();
        apiResponse.setSuccess(true);
        apiResponse.setMessage("Sticker assigned successfully");
        apiResponse.setData(response);
        
        return apiResponse;
    }

    @Operation(description = "Remove a sticker assignment from a user (access controlled based on caller identity)")
    @Path("/award/v1/users/{userId}/stickers/{stickerId}")
    @DELETE
    @Produces("application/json")
    public StickerRemovalResponseApiResponse removeStickerFromUser(@PathParam("userId") String userId,
            @PathParam("stickerId") String stickerId) {
        
        List<StickerDTO> userStickerList = userStickers.get(userId);
        
        if (userStickerList == null) {
            StickerRemovalResponseApiResponse apiResponse = new StickerRemovalResponseApiResponse();
            apiResponse.setSuccess(false);
            apiResponse.setMessage("User not found: " + userId);
            return apiResponse;
        }
        
        boolean removed = userStickerList.removeIf(sticker -> sticker.getStickerId().equals(stickerId));
        
        if (!removed) {
            StickerRemovalResponseApiResponse apiResponse = new StickerRemovalResponseApiResponse();
            apiResponse.setSuccess(false);
            apiResponse.setMessage("Sticker not found for user: " + stickerId);
            return apiResponse;
        }
        
        // Create response
        StickerRemovalResponse response = new StickerRemovalResponse();
        response.setUserId(userId);
        response.setStickerId(stickerId);
        response.setRemovedAt(new Date());
        
        StickerRemovalResponseApiResponse apiResponse = new StickerRemovalResponseApiResponse();
        apiResponse.setSuccess(true);
        apiResponse.setMessage("Sticker removed successfully");
        apiResponse.setData(response);
        
        return apiResponse;
    }
}