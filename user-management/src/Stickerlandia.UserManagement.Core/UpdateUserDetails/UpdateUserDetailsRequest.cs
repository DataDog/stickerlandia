// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.Core.UpdateUserDetails;

public record UpdateUserDetailsRequest
{
    [JsonIgnore]
    public AccountId? AccountId { get; set; }

    public string FirstName { get; set; } = "";
    
    public string LastName { get; set; } = "";

    public bool IsValid()
    {
        return AccountId != null;
    }
}