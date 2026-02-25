/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.Lambda;

public class SqsHandler(ILogger<SqsHandler> logger, IServiceScopeFactory serviceScopeFactory)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    [LambdaFunction]
    public async Task<SQSBatchResponse> StickerClaimed(SQSEvent sqsEvent)
    {
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");
        
        ArgumentNullException.ThrowIfNull(sqsEvent, nameof(sqsEvent));

        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerClaimedHandler>();

        var failedMessages = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var message in sqsEvent.Records) await ProcessMessage(message, failedMessages, handler);

        return new SQSBatchResponse(failedMessages);
    }

    private async Task ProcessMessage(SQSEvent.SQSMessage message,
        List<SQSBatchResponse.BatchItemFailure> failedMessages,
        StickerClaimedHandler handler)
    {
        var evtData = JsonSerializer.Deserialize<CloudWatchEvent<JsonElement>>(message.Body, _jsonSerializerOptions);

        if (evtData == null)
        {
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            return;
        }
        
        var detailBytes = JsonSerializer.SerializeToUtf8Bytes(evtData.Detail, _jsonSerializerOptions);
        var formatter = new JsonEventFormatter<StickerClaimedEventV1>();
        var cloudEvent = await formatter.DecodeStructuredModeMessageAsync(
            new MemoryStream(detailBytes), null, new List<CloudEventAttribute>());
        var stickerClaimed = (StickerClaimedEventV1?)cloudEvent.Data;

        try
        {
            await handler.Handle(stickerClaimed!);
        }
        catch (InvalidUserException ex)
        {
            Log.InvalidUser(logger, ex);
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
        }
    }
}