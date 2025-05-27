package com.datadoghq.stickerlandia.stickeraward.messaging;

import com.datadoghq.stickerlandia.stickeraward.award.messaging.StickerAwardEventPublisher;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.inject.Alternative;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.datadoghq.stickerlandia.stickeraward.award.entity.StickerAssignment;

/**
 * Mock implementation of StickerEventPublisher for tests.
 * This prevents Kafka connectivity issues in tests.
 */
@Alternative
@ApplicationScoped
public class MockStickerAwardEventPublisher extends StickerAwardEventPublisher {
    
    private static final Logger log = LoggerFactory.getLogger(MockStickerAwardEventPublisher.class);
    
    @Override
    public void publishStickerAssigned(StickerAssignment assignment) {
        log.info("MOCK: Publishing sticker assigned event: userId={}, stickerId={}", 
                assignment.getUserId(), assignment.getSticker().getStickerId());
        // No-op in tests
    }
    
    @Override
    public void publishStickerRemoved(StickerAssignment assignment) {
        log.info("MOCK: Publishing sticker removed event: userId={}, stickerId={}", 
                assignment.getUserId(), assignment.getSticker().getStickerId());
        // No-op in tests
    }
} 