// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// This is a class that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.FunctionApp;

internal sealed class OutboxWorkerFunction(
    OutboxProcessor outboxProcessor,
    ILogger<OutboxWorkerFunction> logger)
{
    [Function("OutboxWorker")]
    public async Task RunAsync(
        [TimerTrigger("30 * * * * *")] TimerInfo timerInfo)
    {
        logger.LogInformation("Running outbox timer");
        
        await outboxProcessor.ProcessAsync();
    }
}