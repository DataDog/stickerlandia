/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Allow catch of a generic exception in the worker to ensure the worker failing doesn't crash the entire application.
#pragma warning disable CA1031
// This is a worker service that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Observability;

namespace Stickerlandia.UserManagement.Worker;

internal sealed class StickerClaimedWorker : BackgroundService
{
    private readonly ILogger<OutboxWorker> _logger;
    private readonly IMessagingWorker _messagingWorker;

    public StickerClaimedWorker(ILogger<OutboxWorker> logger, IMessagingWorker messagingWorker)
    {
        _logger = logger;
        _messagingWorker = messagingWorker;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start processing
        await _messagingWorker.StartAsync();

        try
        {
            var failureCount = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _messagingWorker.PollAsync(stoppingToken);
                    failureCount = 0;
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.GenericWarning(_logger, "Error processing message", ex);
                    failureCount++;

                    if (failureCount > 10)
                    {
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.GenericWarning(_logger, "Error processing message", ex);
        }
        finally
        {
            // Stop the processor
            await _messagingWorker.StopAsync(stoppingToken);
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _messagingWorker.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}