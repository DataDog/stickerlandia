/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text;
using System.Text.Json;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

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
            Acks             = Acks.All
        };
    }
    public async Task SendMessageAsync(string queueName, object message)
    {
        using var producer = new ProducerBuilder<string, string>(config).Build();

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = queueName,
            Time = DateTime.UtcNow,
            Data = JsonDocument.Parse(JsonSerializer.Serialize(message)).RootElement
        };
        var formatter = new JsonEventFormatter();
        var encoded = formatter.EncodeStructuredModeMessage(cloudEvent, out _);

        await producer.ProduceAsync(queueName, new Message<string, string>
        {
            Key = "", Value = Encoding.UTF8.GetString(encoded.Span)
        });

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}