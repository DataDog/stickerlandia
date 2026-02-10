// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515

namespace Stickerlandia.PrintService.Client.Models;

/// <summary>
/// Represents the current status of the printer client.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Client is not configured with an API key.
    /// </summary>
    NotConfigured,

    /// <summary>
    /// Client is configured and connected to the backend.
    /// </summary>
    Connected,

    /// <summary>
    /// Client is configured but cannot reach the backend.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Client is configured but authentication failed.
    /// </summary>
    AuthenticationFailed
}
