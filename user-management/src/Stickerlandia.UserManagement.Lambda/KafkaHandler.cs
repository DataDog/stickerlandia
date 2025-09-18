/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.KafkaEvents;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.Lambda;

public class KafkaHandler(ILogger<SqsHandler> logger, IServiceScopeFactory serviceScopeFactory)
{
    [LambdaFunction]
    public async Task StickerClaimed(KafkaEvent kafkaEvent)
    {
        ArgumentNullException.ThrowIfNull(kafkaEvent, nameof(kafkaEvent));
        
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");

        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerClaimedHandler>();

        foreach (var message in kafkaEvent.Records)
        foreach (var record in message.Value)
        {
            var evtData = await JsonSerializer.DeserializeAsync<StickerClaimedEventV1>(record.Value);

            if (evtData == null) continue;

            try
            {
                await handler.Handle(evtData!);
            }
            catch (InvalidUserException ex)
            {
                Log.InvalidUser(logger, ex);
            }
        }
    }
}