/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Agnostic;

public class DatabaseFailureException : Exception
{
    
    public DatabaseFailureException() : base()
    {
    }

    public DatabaseFailureException(string message) : base(message)
    {
    }

    public DatabaseFailureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}