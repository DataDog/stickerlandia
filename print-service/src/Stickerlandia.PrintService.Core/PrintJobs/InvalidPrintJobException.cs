// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core.PrintJobs;

public class InvalidPrintJobException : Exception
{
    public InvalidPrintJobException()
    {
    }

    public InvalidPrintJobException(string message) : base(message)
    {
    }

    public InvalidPrintJobException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
