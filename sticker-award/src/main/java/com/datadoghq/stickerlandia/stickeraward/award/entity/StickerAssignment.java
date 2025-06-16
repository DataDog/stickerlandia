package com.datadoghq.stickerlandia.stickeraward.award.entity;

import com.datadoghq.stickerlandia.stickeraward.sticker.entity.Sticker;
import io.quarkus.hibernate.orm.panache.PanacheEntityBase;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import java.time.Instant;
import java.util.List;

/** Entity representing the assignment of a sticker to a user. */
@Entity
@Table(
        name = "sticker_assignments",
        uniqueConstraints = @UniqueConstraint(columnNames = {"user_id", "sticker_id"}))
public class StickerAssignment extends PanacheEntityBase {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "assignment_id")
    private Long id;

    @Column(name = "user_id", nullable = false)
    private String userId;

    @ManyToOne(fetch = FetchType.EAGER)
    @JoinColumn(name = "sticker_id", nullable = false)
    private Sticker sticker;

    @Column(name = "assigned_at", nullable = false)
    private Instant assignedAt;

    @Column(name = "removed_at")
    private Instant removedAt;

    @Column(name = "reason", length = 500)
    private String reason;

    // Default constructor for JPA
    public StickerAssignment() {}

    /**
     * Constructor with fields for creating a new sticker assignment.
     *
     * @param userId the ID of the user
     * @param sticker the sticker to assign
     * @param reason the reason for the assignment
     */
    public StickerAssignment(String userId, Sticker sticker, String reason) {
        this.userId = userId;
        this.sticker = sticker;
        this.reason = reason;
        this.assignedAt = Instant.now();
    }

    // Getters and setters
    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getUserId() {
        return userId;
    }

    public void setUserId(String userId) {
        this.userId = userId;
    }

    public Sticker getSticker() {
        return sticker;
    }

    public void setSticker(Sticker sticker) {
        this.sticker = sticker;
    }

    public Instant getAssignedAt() {
        return assignedAt;
    }

    public void setAssignedAt(Instant assignedAt) {
        this.assignedAt = assignedAt;
    }

    public Instant getRemovedAt() {
        return removedAt;
    }

    public void setRemovedAt(Instant removedAt) {
        this.removedAt = removedAt;
    }

    public String getReason() {
        return reason;
    }

    public void setReason(String reason) {
        this.reason = reason;
    }

    // Helper methods
    /**
     * Checks if this assignment is currently active (not removed).
     *
     * @return true if the assignment is active, false otherwise
     */
    public boolean isActive() {
        return removedAt == null;
    }

    // Repository methods
    /**
     * Finds all active sticker assignments for a specific user.
     *
     * @param userId the ID of the user
     * @return list of active sticker assignments
     */
    public static List<StickerAssignment> findActiveByUserId(String userId) {
        return list("userId = ?1 AND removedAt IS NULL", userId);
    }

    /**
     * Finds an active sticker assignment for a specific user and sticker.
     *
     * @param userId the ID of the user
     * @param stickerId the ID of the sticker
     * @return the active assignment, or null if not found
     */
    public static StickerAssignment findActiveByUserAndSticker(String userId, String stickerId) {
        return find(
                        "userId = ?1 AND sticker.stickerId = ?2 AND removedAt IS NULL",
                        userId,
                        stickerId)
                .firstResult();
    }

    public static List<StickerAssignment> findActiveByStickerId(String stickerId) {
        return list("sticker.stickerId = ?1 AND removedAt IS NULL", stickerId);
    }

    public static long countActiveByStickerId(String stickerId) {
        return count("sticker.stickerId = ?1 AND removedAt IS NULL", stickerId);
    }
}
