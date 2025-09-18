/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal static class TestConstants
{
    public static string DefaultTestUrl =
        Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "https://localhost:51545";

    // OAuth2.0 Configuration
    public static string OAuth2ClientId = "web-ui";
    public static string OAuth2ClientSecret = "stickerlandia-web-ui-secret-2025";
    public static string OAuth2RedirectUri = "http://localhost:8080/api/app/auth/callback";
    public static string[] OAuth2Scopes = ["offline_access"];
    
    // OAuth2.0 Endpoints
    public static string AuthorizeEndpoint = "api/users/v1/connect/authorize";
    public static string TokenEndpoint = "api/users/v1/connect/token";
    public static string UserInfoEndpoint = "api/users/v1/connect/userinfo";

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