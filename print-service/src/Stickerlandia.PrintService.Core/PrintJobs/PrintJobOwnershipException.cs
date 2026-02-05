// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Exception thrown when a printer tries to acknowledge a job it doesn't own.
/// </summary>
public class PrintJobOwnershipException : Exception
{
    public PrintJobOwnershipException()
    {
    }

    public PrintJobOwnershipException(string message) : base(message)
    {
    }

    public PrintJobOwnershipException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
