/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue.event;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.Instant;

/** Event published when a sticker is removed from the catalogue */
public class StickerDeletedEvent extends DomainEvent {

    public static final String EVENT_NAME = "StickerDeleted";
    public static final String EVENT_VERSION = "v1";
    public static final String EVENT_TYPE = "stickers.stickerDeleted.v1";

    @JsonProperty("stickerId")
    private String stickerId;

    @JsonProperty("name")
    private String name;

    @JsonProperty("deletedAt")
    private String deletedAt;

    public StickerDeletedEvent() {
        super(EVENT_NAME, EVENT_VERSION);
        this.deletedAt = Instant.now().toString();
    }

    public StickerDeletedEvent(String stickerId, String name) {
        this();
        this.stickerId = stickerId;
        this.name = name;
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

    public String getDeletedAt() {
        return deletedAt;
    }

    public void setDeletedAt(Instant deletedAt) {
        this.deletedAt = deletedAt.toString();
    }
}
