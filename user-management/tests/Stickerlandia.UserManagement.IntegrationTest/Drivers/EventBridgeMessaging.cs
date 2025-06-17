// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Confluent.Kafka;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public class EventBridgeMessaging : IMessaging, IAsyncDisposable
{
    private readonly string _environment;
    private readonly AmazonEventBridgeClient client;

    public EventBridgeMessaging(string environment)
    {
        _environment = environment;
        client = new AmazonEventBridgeClient();
    }

    public async Task SendMessageAsync(string queueName, object message)
    {
        await client.PutEventsAsync(new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    EventBusName = $"user-service-{_environment}-bus",
                    Source = $"{_environment}.stickers",
                    DetailType = queueName,
                    Detail = JsonSerializer.Serialize(message)
                }
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        // Nothing to dispose
    }
}