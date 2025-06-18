// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Allow catch of a generic exception in the worker to ensure the worker failing doesn't crash the entire application.
#pragma warning disable CA1031
// This is a worker service that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Worker;

internal sealed class OutboxWorker(OutboxProcessor outboxProcessor)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await outboxProcessor.ProcessAsync();

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}