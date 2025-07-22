// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.AspNetCore.Identity;

namespace Stickerlandia.UserManagement.Core;

public class PostgresUserAccount : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ClaimedStickerCount { get; set; }
    public DateTime DateCreated { get; set; }
    public AccountTier AccountTier { get; set; }
    public AccountType AccountType { get; set; }
}