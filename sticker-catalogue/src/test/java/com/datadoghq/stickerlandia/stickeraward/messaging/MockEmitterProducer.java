package com.datadoghq.stickerlandia.stickeraward.messaging;

import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerAssignedToUserEvent;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerClaimedEvent;
import com.datadoghq.stickerlandia.stickeraward.common.events.out.StickerRemovedFromUserEvent;
import io.quarkus.test.Mock;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.enterprise.inject.Produces;
import org.eclipse.microprofile.reactive.messaging.Emitter;
import org.mockito.Mockito;

/**
 * Producer for mock emitters to be used in tests. This is necessary because we need to mock the
 * Kafka emitters.
 */
@Mock
@ApplicationScoped
public class MockEmitterProducer {

    @Produces
    @jakarta.inject.Named("stickers-assigned")
    public Emitter<StickerAssignedToUserEvent> createStickerAssignedEmitter() {
        return Mockito.mock(Emitter.class);
    }

    @Produces
    @jakarta.inject.Named("stickers-removed")
    public Emitter<StickerRemovedFromUserEvent> createStickerRemovedEmitter() {
        return Mockito.mock(Emitter.class);
    }

    @Produces
    @jakarta.inject.Named("stickers-claimed")
    public Emitter<StickerClaimedEvent> createStickerClaimedEmitter() {
        return Mockito.mock(Emitter.class);
    }
}
