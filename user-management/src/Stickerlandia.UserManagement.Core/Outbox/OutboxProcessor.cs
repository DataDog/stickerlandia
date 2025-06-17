// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Catching generic exceptions is not recommended, but in this case we want to catch all exceptions so that a failure in outbox processing does not crash the application.
#pragma warning disable CA1031
using System.Text.Json;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.Observability;
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

                LogMessages.LogUnprocessedOutboxItems(logger, 5, null);
            }

            catch (Exception ex)
            {
                // Log the exception
                LogMessages.LogErrorProcessingOutboxItems(logger, ex);
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
                            LogMessages.LogOutboxItemDeserializationWarning(logger, item.ItemId, null);
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
                
                LogMessages.LogFailureProcessingOutboxItem(logger, item.ItemId, e);
                item.FailureReason = e.Message;
                item.Failed = true;
            }
        }
        
        await outbox.UpdateOutboxItem(item);
    }
}