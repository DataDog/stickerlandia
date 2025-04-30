// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.AspNet;

public class OutboxWorker(IUserEventPublisher eventPublisher, IOutbox outbox, ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox worker started. Processing unprocessed outbox items every 5 seconds.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var outboxItems = await outbox.GetUnprocessedItemsAsync();

                foreach (var item in outboxItems)
                {
                    try
                    {
                        switch (item.EventType)
                        {
                            case "users.userRegistered.v1":
                                var userRegisteredEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(item.EventData);
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

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}