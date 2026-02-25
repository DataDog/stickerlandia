/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class AzureServiceBusMessaging(string connectionString) : IMessaging, IAsyncDisposable
{
    private readonly ServiceBusClient _client = new(connectionString);

    public async Task SendMessageAsync(string queueName, object message)
    {
        var sender = _client.CreateSender(queueName);

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

        var serviceBusMessage = new ServiceBusMessage(encoded)
        {
            ContentType = "application/cloudevents+json"
        };

        await sender.SendMessageAsync(serviceBusMessage);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}