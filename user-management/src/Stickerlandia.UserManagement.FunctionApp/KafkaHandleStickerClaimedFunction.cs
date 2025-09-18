/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// This is a class that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.FunctionApp;

internal sealed class KafkaHandleStickerClaimedFunction(
    ILogger<KafkaHandleStickerClaimedFunction> logger,
    StickerClaimedHandler stickerClaimedHandler)
{
    [Function(nameof(KafkaHandleStickerClaimedFunction))]
    public async Task Run(
        [KafkaTrigger("ConnectionStrings:messaging", "users.stickerClaimed.v1", ConsumerGroup = "stickerlandia-users",
            IsBatched = true)]
        string[] events,
        FunctionContext context)
    {
        foreach (var @event in events)
        {
            logger.LogInformation("Successfully received message with body: {Body}", @event);

            using var document = JsonDocument.Parse(@event);

            var root = document.RootElement;

            // Access a specific property, e.g., accountId
            var messageBody = root.GetProperty("Value").GetString();

            try
            {
                if (messageBody is null)
                {
                    logger.LogWarning("Message body is null, skipping processing");
                    continue;
                }

                // Parse the message body to get the event
                var eventData = JsonSerializer.Deserialize<StickerClaimedEventV1>(@messageBody);

                if (eventData != null)
                    // Process the event
                    await stickerClaimedHandler.Handle(eventData);
                else
                    logger.LogWarning("Failed to deserialize sticker claimed event");
            }
            catch (InvalidUserException ex)
            {
                logger.LogWarning(ex, "The user does not exist or is invalid for the sticker claimed event");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing sticker claimed event");
                throw;
            }
        }
    }
}