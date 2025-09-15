package com.datadoghq.stickerlandia.stickercatalogue.mapper;

import com.datadoghq.stickerlandia.stickercatalogue.dto.StickerDTO;
import com.datadoghq.stickerlandia.stickercatalogue.entity.Sticker;

public class StickerMapper {

    public static StickerDTO toDTO(Sticker sticker) {
        if (sticker == null) {
            return null;
        }

        StickerDTO dto = new StickerDTO();
        dto.setStickerId(sticker.getStickerId());
        dto.setStickerName(sticker.getName());
        dto.setStickerDescription(sticker.getDescription());
        dto.setImageKey(sticker.getImageKey());
        dto.setStickerQuantityRemaining(sticker.getStickerQuantityRemaining());
        dto.setCreatedAt(sticker.getCreatedAt());
        dto.setUpdatedAt(sticker.getUpdatedAt());

        return dto;
    }
}
