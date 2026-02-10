/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

namespace Stickerlandia.UserManagement.Api.Configurations;

/// <summary>
/// Configuration options for the API rate limiter.
/// </summary>
internal sealed class RateLimitOptions
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Whether rate limiting is enabled. Set to false for load testing.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of requests permitted per window per client IP.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Maximum number of requests that can be queued when the limit is reached.
    /// </summary>
    public int QueueLimit { get; set; } = 10;

    /// <summary>
    /// The time window in seconds for rate limiting.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
