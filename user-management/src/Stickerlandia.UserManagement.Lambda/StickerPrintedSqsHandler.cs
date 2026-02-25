/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.Lambda;

public class StickerPrintedSqsHandler(ILogger<StickerPrintedSqsHandler> logger, IServiceScopeFactory serviceScopeFactory)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    
    [LambdaFunction]
    public async Task<SQSBatchResponse> Handler(SQSEvent sqsEvent)
    {
        using var processSpan = Tracer.Instance.StartActive($"process printJobs.completed.v1");
        
        ArgumentNullException.ThrowIfNull(sqsEvent, nameof(sqsEvent));

        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerPrintedHandler>();

        var failedMessages = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var message in sqsEvent.Records) await ProcessMessage(message, failedMessages, handler);

        return new SQSBatchResponse(failedMessages);
    }

    private async Task ProcessMessage(SQSEvent.SQSMessage message,
        List<SQSBatchResponse.BatchItemFailure> failedMessages,
        StickerPrintedHandler handler)
    {
        var evtData = JsonSerializer.Deserialize<CloudWatchEvent<StickerPrintedEventV1>>(message.Body, _jsonSerializerOptions);

        if (evtData == null)
        {
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            return;
        }

        try
        {
            await handler.Handle(evtData.Detail!);
        }
        catch (InvalidUserException ex)
        {
            Log.InvalidUser(logger, ex);
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
        }
    }
}