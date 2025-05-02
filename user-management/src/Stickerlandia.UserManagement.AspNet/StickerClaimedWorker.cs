// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Azure.Messaging.ServiceBus;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.AspNet;

public class StickerClaimedWorker : BackgroundService
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
                    _logger.LogWarning(ex, "Error running worker");
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
            _logger.LogError(ex, "Error running worker");
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