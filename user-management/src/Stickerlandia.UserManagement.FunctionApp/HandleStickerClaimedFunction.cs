// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// This is a class that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.FunctionApp;

internal sealed class HandleStickerClaimedFunction(
    ILogger<HandleStickerClaimedFunction> logger,
    StickerClaimedHandler stickerClaimedHandler)
{
    [Function(nameof(HandleStickerClaimedFunction))]
    public async Task Run(
        [ServiceBusTrigger("users.stickerClaimed.v1", Connection = "ConnectionStrings:messaging")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        logger.LogInformation("Successfully received message with body: {Body}", message.Body);

        try
        {
            // Parse the message body to get the event
            var eventData = JsonSerializer.Deserialize<StickerClaimedEventV1>(message.Body);

            if (eventData != null)
            {
                // Process the event
                await stickerClaimedHandler.Handle(eventData);

                // Complete the message
                await messageActions.CompleteMessageAsync(message);
            }
            else
            {
                logger.LogWarning("Failed to deserialize sticker claimed event");
                await messageActions.DeadLetterMessageAsync(message);
            }
        }
        catch (InvalidUserException ex)
        {
            logger.LogWarning(ex, "The user does not exist or is invalid for the sticker claimed event");
            await messageActions.DeadLetterMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing sticker claimed event");
            await messageActions.DeadLetterMessageAsync(message);
            throw;
        }
    }
}