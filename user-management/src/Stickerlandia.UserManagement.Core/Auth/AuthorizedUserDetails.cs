// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Core.Auth;

public record AuthorizedUserDetails
{
    public AccountId? AccountId { get; set; }
    
    public string Role { get; set; } = "";
    
    public string UserTier { get; set; } = "";
}