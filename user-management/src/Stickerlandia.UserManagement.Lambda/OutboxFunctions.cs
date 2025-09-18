/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Amazon.Lambda.Annotations;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Lambda;

public class OutboxFunctions(ILogger<SqsHandler> logger, OutboxProcessor outboxProcessor)
{
    [LambdaFunction]
    public async Task Worker(object evtData)
    {
        Log.StartingMessageProcessor(logger, "outbox-worker");
        
        await outboxProcessor.ProcessAsync();
    }
}