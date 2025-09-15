package com.datadoghq.stickerlandia.stickercatalogue.result;

import com.datadoghq.stickerlandia.stickercatalogue.entity.Sticker;

/**
 * Result type for sticker operations that can either succeed with a sticker or fail because the
 * sticker is not found.
 */
public sealed interface StickerResult permits StickerResult.Success, StickerResult.NotFound {

    record Success(Sticker sticker) implements StickerResult {}

    record NotFound(String stickerId) implements StickerResult {}
}
