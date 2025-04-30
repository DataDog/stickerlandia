// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Datadog.Trace;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.FunctionApp;

public class OutboxWorkerFunction(
    IUserEventPublisher eventPublisher,
    IOutbox outbox,
    ILogger<OutboxWorkerFunction> logger)
{
    [Function("OutboxWorker")]
    public async Task RunAsync(
        [TimerTrigger("30 * * * * *")] TimerInfo timerInfo)
    {
        logger.LogInformation("Running outbox timer");
        
        using (var outboxProcessingScope = Tracer.Instance.StartActive("outbox worker"))
        {
            try
            {
                var outboxItems = await outbox.GetUnprocessedItemsAsync();

                outboxProcessingScope.Span.SetTag("outbox.items.count", outboxItems.Count);

                foreach (var item in outboxItems)
                {
                    try
                    {
                        switch (item.EventType)
                        {
                            case "users.userRegistered.v1":
                                var userRegisteredEvent =
                                    JsonSerializer.Deserialize<UserRegisteredEvent>(item.EventData);
                                if (userRegisteredEvent == null)
                                {
                                    logger.LogWarning("Contents of outbox item cannot be deserialized {ItemId}",
                                        item.ItemId);
                                    item.FailureReason = "Contents of outbox item cannot be deserialized.";
                                    item.Failed = true;
                                    break;
                                }

                                await eventPublisher.PublishUserRegisteredEventV1(userRegisteredEvent);
                                item.Processed = true;
                                break;
                            default:
                                item.Failed = true;
                                item.FailureReason = "Unknown event type";
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failure processing outbox item {ItemId}", item.ItemId);
                        item.FailureReason = e.Message;
                        item.Failed = true;
                    }

                    await outbox.UpdateOutboxItem(item);
                }

                logger.LogInformation("There are {Count} unprocessed outbox items", outboxItems.Count);
            }
            catch (Exception ex)
            {
                // Log the exception
                logger.LogError(ex, $"Error processing outbox items: {ex.Message}");
            }
        }
    }
}