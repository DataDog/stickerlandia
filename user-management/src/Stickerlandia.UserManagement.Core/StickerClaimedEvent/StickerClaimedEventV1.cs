// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.Core.StickerClaimedEvent;

public record StickerClaimedEventV1
{
    [JsonPropertyName("accountId")] 
    public string AccountId {get;init;}
    
    [JsonPropertyName("stickerId")]
    public string StickerId { get; init; }
}