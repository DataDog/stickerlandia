// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Command to acknowledge a print job as completed or failed.
/// </summary>
public record AcknowledgePrintJobCommand
{
    /// <summary>
    /// The ID of the print job to acknowledge.
    /// </summary>
    [JsonPropertyName("printJobId")]
    public string PrintJobId { get; init; } = string.Empty;

    /// <summary>
    /// Whether the job completed successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// The reason for failure if Success is false.
    /// </summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    /// <summary>
    /// The printer ID making the acknowledgment (set from authentication).
    /// </summary>
    [JsonIgnore]
    public string PrinterId { get; init; } = string.Empty;

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(PrintJobId))
        {
            return false;
        }

        if (!Success && string.IsNullOrEmpty(FailureReason))
        {
            return false;
        }

        return true;
    }
}
