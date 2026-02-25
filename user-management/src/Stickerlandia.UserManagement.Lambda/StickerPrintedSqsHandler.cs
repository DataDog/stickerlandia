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
using Stickerlandia.UserManagement.Core.RegisterUser;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.Lambda;

public class StickerPrintedSqsHandler(
    ILogger<StickerPrintedSqsHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    [LambdaFunction]
    public async Task<SQSBatchResponse> Handler(SQSEvent sqsEvent)
    {
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
        var evtData = JsonSerializer.Deserialize<CloudWatchEvent<JsonElement>>(message.Body, _jsonSerializerOptions);

        if (evtData == null)
        {
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            return;
        }

        // The detail field contains the CloudEvent in structured mode format.
        // Re-serialize it to bytes so the CloudEvents formatter can decode it.
        var detailBytes = JsonSerializer.SerializeToUtf8Bytes(evtData.Detail, _jsonSerializerOptions);
        var formatter = new JsonEventFormatter<StickerPrintedEventV1>();
        var cloudEvent = await formatter.DecodeStructuredModeMessageAsync(
            new MemoryStream(detailBytes), null, new List<CloudEventAttribute>());
        var stickerPrinted = (StickerPrintedEventV1?)cloudEvent.Data;

        if (stickerPrinted == null)
        {
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            return;
        }

        // Extract trace context injected by the publisher.
        // Datadog headers (x-datadog-trace-id, etc.) are in the _datadog object at the root of the
        // CloudEvent JSON. W3C traceparent is set as a CloudEvent extension attribute.
        var propagatedContext = new SpanContextExtractor().ExtractIncludingDsm(
            (cloudEvent, evtData.Detail),
            static (carrier, key) =>
            {
                var (ce, detail) = carrier;
                if (detail.TryGetProperty("_datadog", out var ddObj) &&
                    ddObj.TryGetProperty(key, out var ddVal))
                {
                    return [ddVal.GetString()];
                }
                var ceVal = ce[key]?.ToString();
                return ceVal is null ? [] : (IEnumerable<string?>)[ceVal];
            },
            "eventbridge",
            cloudEvent.Type!);

        using var processSpan = Tracer.Instance.StartActive(
            $"process {cloudEvent.Type}",
            new SpanCreationSettings { Parent = propagatedContext });

        try
        {
            await handler.Handle(stickerPrinted);
        }
        catch (InvalidUserException ex)
        {
            Log.InvalidUser(logger, ex);
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
        }
    }
}