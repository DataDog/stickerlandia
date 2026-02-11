/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

#pragma warning disable CA1812

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.IntegrationTest.ViewModels;

internal sealed record SubmitPrintJobResponse
{
    [JsonPropertyName("printJobId")]
    public string PrintJobId { get; init; } = string.Empty;
}
