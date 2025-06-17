package com.datadoghq.stickerlandia.stickeraward.award.messaging;

import com.datadoghq.stickerlandia.stickeraward.common.events.in.CertificationCompletedEvent;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.inject.Inject;
import org.eclipse.microprofile.reactive.messaging.Incoming;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/** Handler for certification-related events received from external services. */
@ApplicationScoped
public class CertificationEventHandler {

    private static final Logger log = LoggerFactory.getLogger(CertificationEventHandler.class);

    @Inject
    ObjectMapper objectMapper;

    /**
     * Processes certification completed events to automatically assign appropriate stickers.
     *
     * @param eventJson The certification completed CloudEvent as JSON string
     */
    @Incoming("certifications-completed")
    public void processCertificationCompletedEvent(String eventJson) {
        try {
            CertificationCompletedEvent event = 
                    objectMapper.readValue(eventJson, CertificationCompletedEvent.class);
            
            log.info("Processing certification completed event: accountId={}, "
                    + "certificationId={}, completedAt={}",
                    event.getAccountId(),
                    event.getCertificationId(),
                    event.getCompletedAt());

            // TODO: Implement automatic sticker assignment logic based on certification type
            // This would typically involve:
            // 1. Mapping certification types to sticker types
            // 2. Checking if user already has the sticker
            // 3. Creating new StickerAssignment if needed
            // 4. Publishing StickerAssignedToUserEvent
            
            log.warn("Automatic sticker assignment not yet implemented for certification: {}", 
                    event.getCertificationId());
                    
        } catch (Exception e) {
            log.error("Error processing certification completed event", e);
        }
    }
}