// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Exception thrown when a print job is not found.
/// </summary>
public class PrintJobNotFoundException : Exception
{
    public PrintJobNotFoundException()
    {
    }

    public PrintJobNotFoundException(string message) : base(message)
    {
    }

    public PrintJobNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
