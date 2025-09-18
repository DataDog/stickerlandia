/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Collections.Immutable;
using System.Security.Claims;

namespace Stickerlandia.UserManagement.Core;

public interface IAuthService
{
    /// <summary>
    /// Verify a user's credentials and return a ClaimsIdentity if successful.
    /// </summary>
    /// <param name="username">The username to verify.</param>
    /// <param name="password">The password to attempt to login with.</param>
    /// <param name="scopes">Available application scopes for the claim.</param>
    /// <returns>A valid claims identity if success, or null if fails. Throws a <see cref="LoginFailedException"/> if the user cannot login.</returns>
    Task<ClaimsIdentity?> VerifyPassword(string username, string password, ImmutableArray<string> scopes);
}