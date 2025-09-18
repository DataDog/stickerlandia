/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1812 // Used for JSON manipulation

using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

internal sealed record UserAccountDTO
{
    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";
    
    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = "";
    
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("claimedStickerCount")]
    public int ClaimedStickerCount { get; set; }
}