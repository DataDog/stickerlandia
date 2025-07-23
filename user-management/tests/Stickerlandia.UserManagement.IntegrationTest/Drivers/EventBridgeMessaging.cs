// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class EventBridgeMessaging : IMessaging, IAsyncDisposable
{
    private readonly string _environment;
    private readonly AmazonEventBridgeClient _client;

    public EventBridgeMessaging(string environment)
    {
        _environment = environment;
        _client = new AmazonEventBridgeClient();
    }

    public async Task SendMessageAsync(string queueName, object message)
    {
        await _client.PutEventsAsync(new PutEventsRequest
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

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        
        GC.SuppressFinalize(this);
        
        return ValueTask.CompletedTask;
    }
}