package com.datadoghq.stickerlandia.stickeraward.messaging;

import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.inject.Alternative;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.datadoghq.stickerlandia.stickeraward.entity.StickerAssignment;

/**
 * Mock implementation of StickerEventPublisher for tests.
 * This prevents Kafka connectivity issues in tests.
 */
@Alternative
@ApplicationScoped
public class MockStickerEventPublisher extends StickerEventPublisher {
    
    private static final Logger log = LoggerFactory.getLogger(MockStickerEventPublisher.class);
    
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