/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025 Datadog, Inc.
 */
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
