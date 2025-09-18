/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1812 // Used for JSON manipulation

using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

internal sealed record ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}