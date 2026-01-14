/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Api;

internal sealed record ApiResponse<T>
{
    public ApiResponse(T body)
    {
        Data = body;
        Success = true;
        Message = "OK";
    }

    public ApiResponse(bool isSuccess, T body, string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        Data = body;
        Success = isSuccess;
        Message = message;
        StatusCode = statusCode;
    }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")] 
    public string Message { get; set; }
    
    [JsonIgnore]
    public HttpStatusCode StatusCode { get; private set; }

    [JsonPropertyName("data")] public T Data { get; set; }
}