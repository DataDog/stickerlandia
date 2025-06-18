// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// This is a class that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Stickerlandia.UserManagement.Api.Configurations;
/// <summary>
/// Operation filter to add authorization responses and security requirements to Swagger operations.
/// </summary>
internal sealed class AuthorizeOperationFilter
    : IOperationFilter
{
    internal static readonly string[] item = new[] { "OAuth2" };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context.MethodInfo.DeclaringType));
        var authAttributes = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>()
            .ToList();

        if (authAttributes.Count > 0)
        {
            operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), new OpenApiResponse { Description = nameof(HttpStatusCode.Unauthorized) });
            operation.Responses.Add(StatusCodes.Status403Forbidden.ToString(), new OpenApiResponse { Description = nameof(HttpStatusCode.Forbidden) });
        }
        
        if (authAttributes.Count > 0)
        {
            operation.Security = new List<OpenApiSecurityRequirement>();

            var oauth2SecurityScheme = new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
            };


            operation.Security.Add(new OpenApiSecurityRequirement()
            {
                [oauth2SecurityScheme] = item
            });
        }
    }
}