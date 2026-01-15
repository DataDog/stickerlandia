// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Suppress code analysis for client application types
#pragma warning disable CA1515, CA1056

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Client.Configuration;

/// <summary>
/// Configuration for the printer client application.
/// </summary>
public sealed class PrinterClientConfig
{
    /// <summary>
    /// The API key used to authenticate with the backend.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// The URL of the backend API.
    /// </summary>
    [JsonPropertyName("backendUrl")]
    public string BackendUrl { get; set; } = "https://api.stickerlandia.com";

    /// <summary>
    /// The interval in seconds between polling requests.
    /// </summary>
    [JsonPropertyName("pollingIntervalSeconds")]
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// The maximum number of jobs to retrieve per poll.
    /// </summary>
    [JsonPropertyName("maxJobsPerPoll")]
    public int MaxJobsPerPoll { get; set; } = 10;

    /// <summary>
    /// The path for local storage of job metadata.
    /// </summary>
    [JsonPropertyName("localStoragePath")]
    public string LocalStoragePath { get; set; } = "./print-jobs";

    /// <summary>
    /// Whether the configuration is complete and valid.
    /// </summary>
    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(BackendUrl);
}
