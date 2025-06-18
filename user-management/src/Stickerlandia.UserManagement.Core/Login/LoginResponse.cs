// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.Core.Login;

public class LoginResponse
{
    [JsonPropertyName("identity")]
    public ClaimsIdentity? Identity { get; set; }
}
