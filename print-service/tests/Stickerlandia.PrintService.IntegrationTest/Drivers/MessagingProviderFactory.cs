/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.IntegrationTest.Drivers;

internal static class MessagingProviderFactory
{
    public static IMessaging From(string hostOn, string? connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        return hostOn switch
        {
            "AZURE" => new AzureServiceBusMessaging(connectionString),
            "AGNOSTIC" => new KafkaMessaging(connectionString),
            "GCP" => new GooglePubSubMessaging(connectionString),
            "AWS" => new EventBridgeMessaging(Environment.GetEnvironmentVariable("ENV") ?? "dev"),
            _ => throw new NotSupportedException($"Unsupported messaging provider: {connectionString}")
        };
    }
}