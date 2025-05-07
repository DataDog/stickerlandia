// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public static class MessagingProviderFactory
{
    public static IMessaging From(string hostOn, string? connectionString)
    {
        if (connectionString == null)
        {
            throw new ArgumentNullException(nameof(connectionString));
        }
        
        if (hostOn == "AZURE")
        {
            return new AzureServiceBusMessaging(connectionString);
        }
        else if (hostOn == "AGNOSTIC")
        {
            return new KafkaMessaging(connectionString);
        }
        else
        {
            throw new NotSupportedException($"Unsupported messaging provider: {connectionString}");
        }
    }
}