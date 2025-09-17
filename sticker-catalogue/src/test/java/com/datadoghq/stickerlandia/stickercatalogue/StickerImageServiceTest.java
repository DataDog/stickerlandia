/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025 Datadog, Inc.
 */
package com.datadoghq.stickerlandia.stickercatalogue;

import static java.nio.charset.StandardCharsets.UTF_8;
import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import io.quarkus.test.junit.QuarkusTest;
import jakarta.inject.Inject;
import java.io.ByteArrayInputStream;
import java.io.InputStream;
import org.junit.jupiter.api.MethodOrderer;
import org.junit.jupiter.api.Order;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestMethodOrder;
import software.amazon.awssdk.services.s3.model.NoSuchKeyException;

@QuarkusTest
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
class StickerImageServiceTest {

    @Inject StickerImageService stickerImageService;

    private static String uploadedImageKey;

    @Test
    @Order(1)
    void testUploadImage() {
        byte[] testImageData = "fake-png-image-data".getBytes(UTF_8);
        InputStream imageStream = new ByteArrayInputStream(testImageData);

        String imageKey =
                stickerImageService.uploadImage(imageStream, "image/png", testImageData.length);

        assertNotNull(imageKey);
        assertTrue(imageKey.startsWith("stickers/"));

        uploadedImageKey = imageKey;
    }

    @Test
    @Order(2)
    void testGetImage() {
        assertNotNull(uploadedImageKey, "Must run upload test first");

        InputStream retrievedImage = stickerImageService.getImage(uploadedImageKey);

        assertNotNull(retrievedImage);

        byte[] retrievedData = assertDoesNotThrow(() -> retrievedImage.readAllBytes());
        String retrievedContent = new String(retrievedData, UTF_8);
        assertEquals("fake-png-image-data", retrievedContent);
    }

    @Test
    @Order(3)
    void testGetImageUrl() {
        assertNotNull(uploadedImageKey, "Must run upload test first");

        String imageUrl = stickerImageService.getImageUrl(uploadedImageKey);

        assertNotNull(imageUrl);
        assertTrue(imageUrl.contains(uploadedImageKey));
    }

    @Test
    @Order(4)
    void testDeleteImage() {
        assertNotNull(uploadedImageKey, "Must run upload test first");

        assertDoesNotThrow(() -> stickerImageService.deleteImage(uploadedImageKey));

        assertThrows(
                NoSuchKeyException.class, () -> stickerImageService.getImage(uploadedImageKey));
    }

    @Test
    void testGetNonExistentImage() {
        String nonExistentKey = "stickers/non-existent-image";

        assertThrows(NoSuchKeyException.class, () -> stickerImageService.getImage(nonExistentKey));
    }

    @Test
    void testUploadEmptyImage() {
        byte[] emptyData = new byte[0];
        InputStream emptyStream = new ByteArrayInputStream(emptyData);

        String imageKey = stickerImageService.uploadImage(emptyStream, "image/png", 0);

        assertNotNull(imageKey);
        assertTrue(imageKey.startsWith("stickers/"));

        InputStream retrievedImage = stickerImageService.getImage(imageKey);
        byte[] retrievedData = assertDoesNotThrow(() -> retrievedImage.readAllBytes());
        assertEquals(0, retrievedData.length);

        stickerImageService.deleteImage(imageKey);
    }

    @Test
    void testUploadLargeImage() {
        byte[] largeImageData = new byte[1024 * 1024];
        for (int i = 0; i < largeImageData.length; i++) {
            largeImageData[i] = (byte) (i % 256);
        }

        InputStream imageStream = new ByteArrayInputStream(largeImageData);

        String imageKey =
                stickerImageService.uploadImage(imageStream, "image/png", largeImageData.length);

        assertNotNull(imageKey);
        assertTrue(imageKey.startsWith("stickers/"));

        InputStream retrievedImage = stickerImageService.getImage(imageKey);
        byte[] retrievedData = assertDoesNotThrow(() -> retrievedImage.readAllBytes());
        assertEquals(largeImageData.length, retrievedData.length);
        assertArrayEquals(largeImageData, retrievedData);

        stickerImageService.deleteImage(imageKey);
    }
}
