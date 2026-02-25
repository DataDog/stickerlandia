/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.DependencyInjection;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;

#pragma warning disable CA1848

namespace Stickerlandia.UserManagement.GCP;

public class GooglePubSubStickerPrintedWorker(
    ILogger<GooglePubSubStickerPrintedWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    [FromKeyedServices("printJobs.completed.v1")]
    SubscriberClient subscriber) : IMessagingWorker
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    
    private Task? _task;
    
    public Task StartAsync()
    {
        logger.LogInformation("GooglePubSubStickerPrintedWorker started");
        
        _task = subscriber.StartAsync(ProcessMessageAsync);
        
        return Task.CompletedTask;
    }

    public async Task PollAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Run for 5 seconds.
            await Task.Delay(5000, stoppingToken);

            if (_task is not null)
            {
                await _task;
            }
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GooglePubSubStickerPrintedWorker failed");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("GooglePubSubStickerPrintedWorker stopping");

        await subscriber.StopAsync(cancellationToken);
    }

    [Channel("printJobs.completed.v1")]
    [SubscribeOperation(typeof(StickerPrintedEventV1))]
    private async Task<SubscriberClient.Reply> ProcessMessageAsync(PubsubMessage message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("GooglePubSubStickerPrintedWorker processing message");
        // Process the message here
        var messageText = message.Data.ToStringUtf8();

        using var processSpan = Tracer.Instance.StartActive($"process printJobs.completed.v1");

        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerPrintedHandler>();
        
        var detailBytes = JsonSerializer.SerializeToUtf8Bytes(messageText, _jsonSerializerOptions);
        var formatter = new JsonEventFormatter<StickerPrintedEventV1>();
        var cloudEvent = await formatter.DecodeStructuredModeMessageAsync(
            new MemoryStream(detailBytes), null, new List<CloudEventAttribute>());
        var stickerPrinted = (StickerPrintedEventV1?)cloudEvent.Data;

        if (stickerPrinted == null)
        {
            processSpan.Span.SetTag("error.message", "Invalid message format");
            return SubscriberClient.Reply.Ack;
        }

        try
        {
            await handler.Handle(stickerPrinted!);
        }
        catch (InvalidUserException ex)
        {
            Log.InvalidUser(logger, ex);

            return SubscriberClient.Reply.Nack;
        }

        // Acknowledge the message
        return SubscriberClient.Reply.Ack;
    }
}