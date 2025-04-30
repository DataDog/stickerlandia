// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Core;

public class InvalidUserException : Exception
{
    
    public InvalidUserException() : base()
    {
    }
    
    public InvalidUserException(string reason)
        : base($"Invalid user: {reason}")
    {
        Reason = reason;
    }

    public InvalidUserException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    public string Reason { get; set; }
}
