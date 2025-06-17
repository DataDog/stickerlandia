// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.


using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Stickerlandia.UserManagement.Api.Configurations;

public class RemoveSwaggerDefinitionsFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Components.Schemas.Remove(nameof(Claim));
        swaggerDoc.Components.Schemas.Remove(nameof(ClaimsIdentity));
        swaggerDoc.Components.Schemas.Remove(nameof(ClaimsPrincipal));
    }
}