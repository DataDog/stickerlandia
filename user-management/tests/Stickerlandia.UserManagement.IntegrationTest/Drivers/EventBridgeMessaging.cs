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
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class EventBridgeMessaging(string environment) : IMessaging, IAsyncDisposable
{
    private readonly AmazonEventBridgeClient _client = new();

    public async Task SendMessageAsync(string queueName, object message)
    {
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

        await _client.PutEventsAsync(new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    EventBusName = $"Stickerlandia-Shared-{environment}",
                    Source = $"{environment}.stickers",
                    DetailType = queueName,
                    Detail = Encoding.UTF8.GetString(encoded.Span)
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