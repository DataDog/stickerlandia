// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.OpenApi.Models;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using Stickerlandia.UserManagement.Azure;
using SecuritySchemeType = Microsoft.OpenApi.Models.SecuritySchemeType;

namespace Stickerlandia.UserManagement.Api.Configurations;

internal static class DocumentationConfig
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
            options.OperationFilter<AuthorizeOperationFilter>();

            // Include XML comments for Swagger
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlFile in xmlFiles) options.IncludeXmlComments(xmlFile);
            
            // This manually removes some types from the auth-gen Swagger definitions that aren't required.
            options.DocumentFilter<RemoveSwaggerDefinitionsFilter>();
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "OAuth 2.0",
                Name = "oauth2",
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Password = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri($"http://localhost:5139/connect/authorize"),
                        TokenUrl = new Uri("http://localhost:5139/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { "User", "Read" }
                        }
                    },
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"/connect/authorize"),
                        TokenUrl = new Uri("/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { "User", "Read" }
                        }
                    }
                }
            });
        });

        return builder;
    }
    
}