package com.datadoghq.stickerlandia.stickeraward.sticker.entity;

import io.quarkus.hibernate.orm.panache.PanacheEntityBase;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;

/** Entity representing a sticker in the system. */
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

    @Column(name = "image_key")
    private String imageKey;

    @Column(name = "sticker_quantity_remaining", nullable = false)
    private Integer stickerQuantityRemaining;

    @Column(name = "created_at", nullable = false)
    private Instant createdAt;

    @Column(name = "updated_at")
    private Instant updatedAt;

    /** Default constructor for JPA. */
    public Sticker() {}

    /**
     * Constructor with fields for creating a new sticker.
     *
     * @param stickerId the unique identifier for the sticker
     * @param name the name of the sticker
     * @param description the description of the sticker
     * @param imageUrl the URL of the sticker image
     * @param stickerQuantityRemaining the quantity remaining for the sticker
     */
    public Sticker(
            String stickerId,
            String name,
            String description,
            String imageUrl,
            Integer stickerQuantityRemaining) {
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

    public String getImageKey() {
        return imageKey;
    }

    public void setImageKey(String imageKey) {
        this.imageKey = imageKey;
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

    /**
     * Checks if this sticker is available for assignment.
     *
     * @return true if the sticker is available, false otherwise
     */
    public boolean isAvailable() {
        return stickerQuantityRemaining == null
                || stickerQuantityRemaining == -1
                || stickerQuantityRemaining > 0;
    }

    /**
     * Checks if this sticker has unlimited quantity.
     *
     * @return true if the sticker has unlimited quantity, false otherwise
     */
    public boolean hasUnlimitedQuantity() {
        return stickerQuantityRemaining == null || stickerQuantityRemaining == -1;
    }

    /** Decrements the quantity remaining by 1. */
    public void decrementQuantity() {
        if (stickerQuantityRemaining != null && stickerQuantityRemaining > 0) {
            stickerQuantityRemaining--;
        }
    }

    /** Increases the quantity remaining by 1 (for removal). */
    public void increaseQuantity() {
        if (stickerQuantityRemaining != null && stickerQuantityRemaining >= 0) {
            stickerQuantityRemaining++;
        }
        // If -1 (infinite), do nothing
    }

    /**
     * Updates the quantity remaining for this sticker.
     *
     * @param newQuantity the new quantity remaining
     */
    public void updateQuantityRemaining(Integer newQuantity) {
        this.stickerQuantityRemaining = newQuantity;
    }

    /**
     * Finds a sticker by its unique identifier.
     *
     * @param stickerId the unique identifier of the sticker
     * @return the sticker if found, null otherwise
     */
    public static Sticker findByStickerId(String stickerId) {
        return find("stickerId", stickerId).firstResult();
    }
}
