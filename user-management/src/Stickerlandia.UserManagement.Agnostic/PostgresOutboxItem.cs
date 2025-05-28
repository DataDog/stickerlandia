// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Agnostic;

public class PostgresOutboxItem
{
    public string Id { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; }
    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
    public string? TraceId { get; set; }
}