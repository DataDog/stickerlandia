// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515, CA1003

namespace Stickerlandia.PrintService.Client.Configuration;

/// <summary>
/// Service for managing printer client configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    PrinterClientConfig Current { get; }

    /// <summary>
    /// Gets whether the client is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Loads configuration from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves configuration to storage.
    /// </summary>
    Task SaveAsync(PrinterClientConfig config);

    /// <summary>
    /// Event raised when configuration changes.
    /// </summary>
    event Action? OnConfigurationChanged;
}
