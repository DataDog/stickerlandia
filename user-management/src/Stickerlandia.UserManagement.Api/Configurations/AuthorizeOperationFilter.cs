// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

<<<<<<< chore/update-user-docs
=======
// This is a class that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

>>>>>>> main
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Stickerlandia.UserManagement.Api.Configurations;

<<<<<<< chore/update-user-docs
/// <summary>
/// Operation filter to add authorization responses and security requirements to Swagger operations.
/// </summary>
internal class AuthorizeOperationFilter
    : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>();

        if (authAttributes.Any())
=======
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
>>>>>>> main
        {
            operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), new OpenApiResponse { Description = nameof(HttpStatusCode.Unauthorized) });
            operation.Responses.Add(StatusCodes.Status403Forbidden.ToString(), new OpenApiResponse { Description = nameof(HttpStatusCode.Forbidden) });
        }

<<<<<<< chore/update-user-docs
        if (authAttributes.Any())
=======
        if (authAttributes.Count > 0)
>>>>>>> main
        {
            operation.Security = new List<OpenApiSecurityRequirement>();

            var oauth2SecurityScheme = new OpenApiSecurityScheme()
            {
<<<<<<< chore/update-user-docs
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
=======
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" },
>>>>>>> main
            };


            operation.Security.Add(new OpenApiSecurityRequirement()
            {
<<<<<<< chore/update-user-docs
                [oauth2SecurityScheme] = new[] { "oauth2" }
=======
                [oauth2SecurityScheme] = item
>>>>>>> main
            });
        }
    }
}