package com.datadoghq.stickerlandia.stickercatalogue.result;

import com.datadoghq.stickerlandia.stickercatalogue.dto.StickerImageUploadResponse;

/** Result type for image upload operations. */
public sealed interface ImageUploadResult
        permits ImageUploadResult.Success, ImageUploadResult.StickerNotFound {

    record Success(StickerImageUploadResponse response) implements ImageUploadResult {}

    record StickerNotFound(String stickerId) implements ImageUploadResult {}
}
