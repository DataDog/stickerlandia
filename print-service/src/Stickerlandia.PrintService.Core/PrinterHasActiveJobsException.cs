// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

namespace Stickerlandia.PrintService.Core;

public class PrinterHasActiveJobsException : Exception
{
    public PrinterHasActiveJobsException() : base()
    {
    }

    public PrinterHasActiveJobsException(string message) : base(message)
    {
    }

    public PrinterHasActiveJobsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
