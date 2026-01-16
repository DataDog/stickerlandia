/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

namespace Stickerlandia.PrintService.Core;

public class PrinterExistsException : Exception
{
    public PrinterExistsException() : base()
    {
    }

    public PrinterExistsException(string message) : base(message)
    {
    }

    public PrinterExistsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}