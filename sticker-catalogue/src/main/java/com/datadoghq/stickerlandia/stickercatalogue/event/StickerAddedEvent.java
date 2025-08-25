package com.datadoghq.stickerlandia.stickercatalogue.event;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.Instant;

/** Event published when a new sticker is added to the catalogue */
public class StickerAddedEvent extends DomainEvent {

    public static final String EVENT_NAME = "StickerAdded";
    public static final String EVENT_VERSION = "v1";
    public static final String EVENT_TYPE = "stickers.stickerAdded.v1";

    @JsonProperty("stickerId")
    private String stickerId;

    @JsonProperty("name")
    private String name;

    @JsonProperty("description")
    private String description;

    @JsonProperty("category")
    private String category;

    @JsonProperty("addedAt")
    private String addedAt;

    public StickerAddedEvent() {
        super(EVENT_NAME, EVENT_VERSION);
        this.addedAt = Instant.now().toString();
    }

    public StickerAddedEvent(String stickerId, String name, String description, String category) {
        this();
        this.stickerId = stickerId;
        this.name = name;
        this.description = description;
        this.category = category;
    }

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

    public String getCategory() {
        return category;
    }

    public void setCategory(String category) {
        this.category = category;
    }

    public String getAddedAt() {
        return addedAt;
    }

    public void setAddedAt(Instant addedAt) {
        this.addedAt = addedAt.toString();
    }
}
