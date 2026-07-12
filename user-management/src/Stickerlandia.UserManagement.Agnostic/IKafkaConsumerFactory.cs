/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Confluent.Kafka;

namespace Stickerlandia.UserManagement.Agnostic;

public interface IKafkaConsumerFactory
{
    IConsumer<string, string> Create(ConsumerConfig config);
}

public sealed class KafkaConsumerFactory : IKafkaConsumerFactory
{
    public IConsumer<string, string> Create(ConsumerConfig config)
        => new ConsumerBuilder<string, string>(config).Build();
}
