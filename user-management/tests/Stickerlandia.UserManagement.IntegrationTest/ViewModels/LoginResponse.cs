/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json.Serialization;

#pragma warning disable CA1812 // Used for JSON manipulation

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

internal sealed record LoginResponse
{
    [JsonPropertyName("authToken")]
    public string AuthToken { get; set; } = "";
}