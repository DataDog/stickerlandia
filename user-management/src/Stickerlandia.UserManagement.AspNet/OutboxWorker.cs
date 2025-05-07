// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.AspNet;

public class OutboxWorker(OutboxProcessor outboxProcessor, ILogger<OutboxWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox worker started. Processing unprocessed outbox items every 5 seconds.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await outboxProcessor.ProcessAsync();

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}