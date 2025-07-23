// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.RegularExpressions;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal static class FormDataExtractor
{
    public static string? ExtractAntiForgeryToken(string html)
    {
        var tokenPattern = @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""[^>]*>";
        var match = Regex.Match(html, tokenPattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}