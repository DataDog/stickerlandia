// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;

#pragma warning disable CA1848

namespace Stickerlandia.UserManagement.GCP;

public class GooglePubSubMessagingWorker(ILogger<GooglePubSubMessagingWorker> logger) : IMessagingWorker
{
    public Task StartAsync()
    {
        logger.LogInformation("GooglePubSubMessagingWorker started");
        
        return Task.CompletedTask;
    }

    public Task PollAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GooglePubSubMessagingWorker polling");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("GooglePubSubMessagingWorker stopping");
        
        return Task.CompletedTask;
    }
}