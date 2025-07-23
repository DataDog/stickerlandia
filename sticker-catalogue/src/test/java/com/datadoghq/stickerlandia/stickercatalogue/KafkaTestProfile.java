package com.datadoghq.stickerlandia.stickercatalogue;

import io.quarkus.test.junit.QuarkusTestProfile;
import java.util.List;

/** Test profile for Kafka integration tests. */
public class KafkaTestProfile implements QuarkusTestProfile {

    @Override
    public List<TestResourceEntry> testResources() {
        return List.of(new TestResourceEntry(KafkaTestResourceLifecycleManager.class));
    }
}
