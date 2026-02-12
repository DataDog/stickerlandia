/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

#pragma warning disable CA1031 // Catch general exceptions - stream processing must report per-record failures

using System.Diagnostics;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.DynamoDBEvents;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.AWS;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.Lambda;

public sealed partial class OutboxFunctions(
    ILogger<OutboxFunctions> logger,
    OutboxProcessor outboxProcessor,
    IAmazonEventBridge eventBridgeClient,
    IOptions<AwsConfiguration> awsConfiguration)
{
    [LambdaFunction]
    public async Task Worker(object evtData)
    {
        Log.StartingMessageProcessor(logger, "outbox-worker");

        await outboxProcessor.ProcessAsync();
    }

    [LambdaFunction]
    public async Task<StreamsEventResponse> HandleStream(DynamoDBEvent dynamoDbEvent)
    {
        ArgumentNullException.ThrowIfNull(dynamoDbEvent);

        var batchItemFailures = new List<StreamsEventResponse.BatchItemFailure>();

        foreach (var record in dynamoDbEvent.Records)
        {
            try
            {
                if (!IsOutboxInsert(record))
                {
                    continue;
                }

                var newImage = record.Dynamodb.NewImage;
                var eventType = newImage[OutboxItemAttributes.EventType].S;
                var eventData = newImage[OutboxItemAttributes.EventData].S;
                var itemId = newImage[OutboxItemAttributes.ItemId].S;

                using var activity = PrintJobInstrumentation.ActivitySource.StartActivity(
                    $"publish {eventType}", ActivityKind.Producer);

                // Restore trace context from the original request
                if (newImage.TryGetValue(OutboxItemAttributes.TraceId, out var traceIdAttr) &&
                    !string.IsNullOrEmpty(traceIdAttr.S))
                {
                    activity?.SetTag("traceparent", traceIdAttr.S);
                }

                var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
                {
                    Id = itemId,
                    Source = new Uri("https://stickerlandia.com"),
                    Type = eventType,
                    Time = DateTimeOffset.UtcNow,
                    DataContentType = "application/json",
                    Data = eventData
                };

                var formatter = new JsonEventFormatter();
                var data = formatter.EncodeStructuredModeMessage(cloudEvent, out _);
                var jsonString = System.Text.Encoding.UTF8.GetString(data.Span);

                var response = await eventBridgeClient.PutEventsAsync(new PutEventsRequest
                {
                    Entries =
                    [
                        new PutEventsRequestEntry
                        {
                            EventBusName = awsConfiguration.Value.EventBusName,
                            Source = $"{Environment.GetEnvironmentVariable("ENV") ?? "dev"}.users",
                            DetailType = eventType,
                            Detail = jsonString,
                        }
                    ]
                });

                if (response.FailedEntryCount is > 0)
                {
                    throw new EventBridgePartialFailureException(
                        response.FailedEntryCount.Value,
                        response.Entries.Where(e => !string.IsNullOrEmpty(e.ErrorCode)).ToList());
                }

                activity?.SetStatus(ActivityStatusCode.Ok);
                LogEventPublished(logger, eventType, itemId);
            }
            catch (Exception ex)
            {
                LogRecordProcessingFailed(logger, record.EventID, ex);
                batchItemFailures.Add(new StreamsEventResponse.BatchItemFailure
                {
                    ItemIdentifier = record.EventID
                });
            }
        }

        return new StreamsEventResponse { BatchItemFailures = batchItemFailures };
    }

    private static bool IsOutboxInsert(DynamoDBEvent.DynamodbStreamRecord record)
    {
        if (!string.Equals(record.EventName, "INSERT", StringComparison.Ordinal))
        {
            return false;
        }

        return record.Dynamodb?.NewImage != null
               && record.Dynamodb.NewImage.TryGetValue("PK", out var pk)
               && pk.S.StartsWith(OutboxItemAttributes.PkPrefix, StringComparison.Ordinal);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published outbox event {EventType} with ItemId {ItemId}")]
    private static partial void LogEventPublished(ILogger logger, string eventType, string itemId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process stream record {EventId}")]
    private static partial void LogRecordProcessingFailed(ILogger logger, string? eventId, Exception exception);
}
