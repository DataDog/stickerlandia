// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.OpenApi.Models;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using Stickerlandia.UserManagement.Azure;

namespace Stickerlandia.UserManagement.Api.Configurations;

public static class DocumentationConfig
{
    public static IHostApplicationBuilder AddDocumentationEndpoints(this IHostApplicationBuilder builder)
    {
        var serviceName = "User Management API";
        
        builder.Services.AddAsyncApiSchemaGeneration(options =>
        {
            options.AssemblyMarkerTypes = new[] { typeof(ServiceBusEventPublisher) };
            options.Middleware.UiTitle = serviceName;
            options.AsyncApi = new AsyncApiDocument
            {
                Info = new Info(serviceName, "v1")
            };
        });

// Add API documentation
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = serviceName, Version = "v1" });

            // Include XML comments for Swagger
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlFile in xmlFiles) options.IncludeXmlComments(xmlFile);
        });

        return builder;
    }
    
}