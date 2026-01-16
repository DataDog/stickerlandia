/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Security.Claims;

namespace Stickerlandia.PrintService.Api.Helpers;

internal static class ClaimsPrincipalExtensions
{
    internal static string? GetUserId(this ClaimsPrincipal user)
    {
        var subClaim = user.Claims.FirstOrDefault(c => c.Type == "sub");

        if (subClaim == null) return null;

        return subClaim.Value;
    }
}