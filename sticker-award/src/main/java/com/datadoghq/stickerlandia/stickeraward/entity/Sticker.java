package com.datadoghq.stickerlandia.stickeraward.entity;

import io.quarkus.hibernate.orm.panache.PanacheEntityBase;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

import java.time.Instant;

@Entity
@Table(name = "stickers")
public class Sticker extends PanacheEntityBase {

    @Id
    @Column(name = "sticker_id")
    private String stickerId;

    @Column(name = "name", nullable = false)
    private String name;

    @Column(name = "description", length = 500)
    private String description;

    @Column(name = "image_url")
    private String imageUrl;

    @Column(name = "sticker_quantity_remaining", nullable = false)
    private Integer stickerQuantityRemaining;

    @Column(name = "created_at", nullable = false)
    private Instant createdAt;

    @Column(name = "updated_at")
    private Instant updatedAt;

    // Default constructor for JPA
    public Sticker() {
    }

    // Constructor with fields
    public Sticker(String stickerId, String name, String description, String imageUrl, Integer stickerQuantityRemaining) {
        this.stickerId = stickerId;
        this.name = name;
        this.description = description;
        this.imageUrl = imageUrl;
        this.stickerQuantityRemaining = stickerQuantityRemaining;
        this.createdAt = Instant.now();
    }

    // Getters and setters
    public String getStickerId() {
        return stickerId;
    }

    public void setStickerId(String stickerId) {
        this.stickerId = stickerId;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public String getImageUrl() {
        return imageUrl;
    }

    public void setImageUrl(String imageUrl) {
        this.imageUrl = imageUrl;
    }

    public Integer getStickerQuantityRemaining() {
        return stickerQuantityRemaining;
    }

    public void setStickerQuantityRemaining(Integer stickerQuantityRemaining) {
        this.stickerQuantityRemaining = stickerQuantityRemaining;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(Instant createdAt) {
        this.createdAt = createdAt;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }

    public void setUpdatedAt(Instant updatedAt) {
        this.updatedAt = updatedAt;
    }

    // Helper methods for quantity management
    public boolean isAvailable() {
        return stickerQuantityRemaining == null || stickerQuantityRemaining == -1 || stickerQuantityRemaining > 0;
    }

    public boolean hasUnlimitedQuantity() {
        return stickerQuantityRemaining == null || stickerQuantityRemaining == -1;
    }

    public void decrementQuantity() {
        if (stickerQuantityRemaining != null && stickerQuantityRemaining > 0) {
            stickerQuantityRemaining--;
        }
    }

    // Increase quantity by 1 (for removal)
    public void increaseQuantity() {
        if (stickerQuantityRemaining != null && stickerQuantityRemaining >= 0) {
            stickerQuantityRemaining++;
        }
        // If -1 (infinite), do nothing
    }

    // Helper methods for finding stickers
    public static Sticker findById(String id) {
        return find("stickerId", id).firstResult();
    }
}