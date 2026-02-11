/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

namespace Stickerlandia.PrintService.IntegrationTest.Drivers;

internal static class TestConstants
{
    public static string DefaultTestUrl =
        Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "https://localhost:51545";

    // JWT Test Configuration (must match appsettings.Development.json)
    public static string TestSigningKey = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";
    public static string TestIssuer = "https://stickerlandia.local";
    public static string TestAudience = "print-service";

    public static string DefaultMessagingConnection(string hostOn, string? messagingConnectionString = "")
    {
        if (!string.IsNullOrEmpty(messagingConnectionString)) return messagingConnectionString;

        var messagingConnection = Environment.GetEnvironmentVariable("MESSAGING_ENDPOINT");

        if (!string.IsNullOrEmpty(messagingConnection)) return messagingConnection;

        return hostOn switch
        {
            "AZURE" =>
                "Endpoint=sb://localhost:60001;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            "AGNOSTIC" => "localhost:53477",
            "AWS" => "", // SQS does not require a connection string in this context
            _ => throw new NotSupportedException($"Unsupported messaging provider: {hostOn}")
        };
    }
}