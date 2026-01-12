/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// This is a class that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

using System.Security.Claims;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Stickerlandia.UserManagement.Api.Configurations;

internal sealed class RemoveSwaggerDefinitionsFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc!.Components!.Schemas!.Remove(nameof(Claim));
        swaggerDoc!.Components!.Schemas!.Remove(nameof(ClaimsIdentity));
        swaggerDoc!.Components!.Schemas!.Remove(nameof(ClaimsPrincipal));
    }
}