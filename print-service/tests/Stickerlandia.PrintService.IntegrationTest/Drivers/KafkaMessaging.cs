/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Confluent.Kafka;

namespace Stickerlandia.PrintService.IntegrationTest.Drivers;

internal sealed class KafkaMessaging : IMessaging, IAsyncDisposable
{
    private readonly ProducerConfig config;
    public KafkaMessaging(string connectionString)
    {
        config = new ProducerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = connectionString,
            // Fixed properties
            SecurityProtocol = SecurityProtocol.Plaintext,
            Acks = Acks.All
        };
    }
    public async Task SendMessageAsync(string queueName, object message)
    {
        using var producer = new ProducerBuilder<string, string>(config).Build();

        await producer.ProduceAsync(queueName, new Message<string, string> { Key = "", Value = JsonSerializer.Serialize(message) });

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}