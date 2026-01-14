/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue.messaging;

import java.util.concurrent.CompletionStage;

/**
 * Interface for publishing sticker catalogue events to a messaging system.
 * Implementations may use different messaging backends (Kafka, EventBridge, etc.)
 * selected at runtime via the MESSAGING_PROVIDER configuration.
 */
public interface StickerEventPublisher {

    /**
     * Publishes a sticker added event when a new sticker is created in the catalogue.
     *
     * @param stickerId the ID of the newly created sticker
     * @param name the name of the newly created sticker
     * @param description the description of the newly created sticker
     * @return completion stage for async processing
     */
    CompletionStage<Void> publishStickerAdded(String stickerId, String name, String description);

    /**
     * Publishes a sticker updated event when an existing sticker is modified.
     *
     * @param stickerId the ID of the updated sticker
     * @param name the name of the updated sticker
     * @param description the description of the updated sticker
     * @return completion stage for async processing
     */
    CompletionStage<Void> publishStickerUpdated(String stickerId, String name, String description);

    /**
     * Publishes a sticker deleted event when a sticker is removed from the catalogue.
     *
     * @param stickerId the ID of the deleted sticker
     * @param name the name of the deleted sticker
     * @return completion stage for async processing
     */
    CompletionStage<Void> publishStickerDeleted(String stickerId, String name);
}
