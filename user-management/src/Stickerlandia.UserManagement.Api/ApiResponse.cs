// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using System.Text.Json.Serialization;
using Datadog.Trace;
using Microsoft.Azure.Functions.Worker.Http;

namespace Stickerlandia.UserManagement.Api;

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

    internal async Task<HttpResponseData> WriteResponse(HttpRequestData req)
    {
        var activeSpan = Tracer.Instance.ActiveScope?.Span;

        if (activeSpan != null)
        {
            activeSpan.SetTag("http.status_code", (int)StatusCode);
            activeSpan.SetTag("http.method", req.Method);
        }
        
        var response = req.CreateResponse(StatusCode);
        await response.WriteAsJsonAsync(this);
        return response;
    }
}