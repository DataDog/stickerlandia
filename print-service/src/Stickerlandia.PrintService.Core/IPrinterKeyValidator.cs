// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core;

/// <summary>
/// Validates printer API keys and returns the associated printer if valid.
/// </summary>
public interface IPrinterKeyValidator
{
    /// <summary>
    /// Validates the given API key and returns the printer if found.
    /// </summary>
    /// <param name="key">The API key to validate.</param>
    /// <returns>The printer associated with the key, or null if invalid.</returns>
    Task<Printer?> ValidateKeyAsync(string key);
}
