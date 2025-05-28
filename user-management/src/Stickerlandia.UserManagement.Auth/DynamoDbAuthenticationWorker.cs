// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Stickerlandia.UserManagement.Auth.DynamoDB;

namespace Stickerlandia.UserManagement.Auth;

/// <summary>
/// Background service that initializes DynamoDB tables and creates default OpenIddict applications
/// </summary>
public class DynamoDbAuthenticationWorker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DynamoDbAuthenticationWorker> _logger;

    public DynamoDbAuthenticationWorker(IServiceProvider serviceProvider, ILogger<DynamoDbAuthenticationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // Initialize DynamoDB tables
            await InitializeDynamoDbTablesAsync(scope, cancellationToken);
            
            // Create default OpenIddict applications
            await CreateDefaultApplicationsAsync(scope, cancellationToken);
            
            _logger.LogInformation("DynamoDB authentication infrastructure initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize DynamoDB authentication infrastructure");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InitializeDynamoDbTablesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
        
        var tables = new[]
        {
            "Users", "Roles", "UserClaims", "UserLogins", "UserTokens", "UserRoles", "RoleClaims",
            "OpenIddictApplications", "OpenIddictAuthorizations", "OpenIddictScopes", "OpenIddictTokens"
        };

        foreach (var tableName in tables)
        {
            try
            {
                await dynamoDb.DescribeTableAsync(tableName, cancellationToken);
                _logger.LogDebug("Table {TableName} already exists", tableName);
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogInformation("Creating DynamoDB table: {TableName}", tableName);
                await CreateTableAsync(dynamoDb, tableName, cancellationToken);
            }
        }
    }

    private async Task CreateTableAsync(IAmazonDynamoDB dynamoDb, string tableName, CancellationToken cancellationToken)
    {
        var request = new CreateTableRequest
        {
            TableName = tableName,
            BillingMode = BillingMode.PAY_PER_REQUEST
        };

        // Define key schema based on table type
        switch (tableName)
        {
            case "Users":
            case "Roles":
            case "OpenIddictApplications":
            case "OpenIddictAuthorizations":
            case "OpenIddictScopes":
            case "OpenIddictTokens":
                // Hash key only tables
                request.KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "Id", KeyType = KeyType.HASH }
                };
                request.AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = "Id", AttributeType = ScalarAttributeType.S }
                };
                break;

            case "UserClaims":
            case "UserLogins":
            case "UserTokens":
            case "UserRoles":
            case "RoleClaims":
                // Hash and range key tables
                var hashKey = tableName.StartsWith("User") ? "UserId" : "RoleId";
                var rangeKey = tableName switch
                {
                    "UserClaims" => "ClaimKey",
                    "UserLogins" => "LoginKey",
                    "UserTokens" => "TokenKey",
                    "UserRoles" => "RoleId",
                    "RoleClaims" => "ClaimKey",
                    _ => "Id"
                };

                request.KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = hashKey, KeyType = KeyType.HASH },
                    new() { AttributeName = rangeKey, KeyType = KeyType.RANGE }
                };
                request.AttributeDefinitions = new List<AttributeDefinition>
                {
                    new() { AttributeName = hashKey, AttributeType = ScalarAttributeType.S },
                    new() { AttributeName = rangeKey, AttributeType = ScalarAttributeType.S }
                };
                break;
        }

        await dynamoDb.CreateTableAsync(request, cancellationToken);
        
        // Wait for table to become active
        var tableActive = false;
        var attempts = 0;
        const int maxAttempts = 30;
        
        while (!tableActive && attempts < maxAttempts)
        {
            await Task.Delay(2000, cancellationToken);
            attempts++;
            
            var describeResponse = await dynamoDb.DescribeTableAsync(tableName, cancellationToken);
            tableActive = describeResponse.Table.TableStatus == TableStatus.ACTIVE;
            
            if (!tableActive)
            {
                _logger.LogDebug("Waiting for table {TableName} to become active... (attempt {Attempt}/{MaxAttempts})", 
                    tableName, attempts, maxAttempts);
            }
        }

        if (tableActive)
        {
            _logger.LogInformation("Successfully created table: {TableName}", tableName);
        }
        else
        {
            _logger.LogWarning("Table {TableName} creation may not be complete after {MaxAttempts} attempts", 
                tableName, maxAttempts);
        }
    }

    private async Task CreateDefaultApplicationsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Create user authentication application
        if (await manager.FindByClientIdAsync("user-authentication", cancellationToken) is null)
        {
            _logger.LogInformation("Creating default application: user-authentication");
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "user-authentication",
                ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                }
            }, cancellationToken);
        }

        // Create internal service application
        if (await manager.FindByClientIdAsync("internal-service", cancellationToken) is null)
        {
            _logger.LogInformation("Creating default application: internal-service");
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "internal-service",
                ClientSecret = "8E1167EF-5C44-4209-A803-3A109155FDD3",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                }
            }, cancellationToken);
        }

        _logger.LogInformation("Default OpenIddict applications configured successfully");
    }
} 