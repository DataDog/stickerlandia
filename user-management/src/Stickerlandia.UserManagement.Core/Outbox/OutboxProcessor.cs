// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.RegisterUser;

namespace Stickerlandia.UserManagement.Core.Outbox;

public class OutboxProcessor(IServiceScopeFactory serviceScope, ILogger<OutboxProcessor> logger)
{
    public async Task ProcessAsync()
    {
        using var scope = serviceScope.CreateScope();
        IOutbox outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        IUserEventPublisher eventPublisher = scope.ServiceProvider.GetRequiredService<IUserEventPublisher>();
        
        using (var outboxProcessingScope = Tracer.Instance.StartActive("outbox worker"))
        {
            try
            {
                var outboxItems = await outbox.GetUnprocessedItemsAsync();
                    
                outboxProcessingScope.Span.SetTag("outbox.items.count", outboxItems.Count);

                foreach (var item in outboxItems)
                {
                    await ProcessOutboxItemAsync(outbox, eventPublisher, item);
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
    
    private async Task ProcessOutboxItemAsync(IOutbox outbox, IUserEventPublisher eventPublisher, OutboxItem item)
    {
        using (var messageProcessingScope = Tracer.Instance.StartActive($"process {item.EventType}"))
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
                            messageProcessingScope.Span.SetTag("outbox.item.error", item.FailureReason);
                            break;
                        }

                        await eventPublisher.PublishUserRegisteredEventV1(userRegisteredEvent);
                        item.Processed = true;
                        break;
                    default:
                        item.Failed = true;
                        item.FailureReason = "Unknown event type";
                        messageProcessingScope.Span.SetTag("outbox.item.error", item.FailureReason);
                        break;
                }
            }
            catch (Exception e)
            {
                messageProcessingScope.Span.SetException(e);
                
                logger.LogError(e, "Failure processing outbox item {ItemId}", item.ItemId);
                item.FailureReason = e.Message;
                item.Failed = true;
            }
        }
        
        await outbox.UpdateOutboxItem(item);
    }
}