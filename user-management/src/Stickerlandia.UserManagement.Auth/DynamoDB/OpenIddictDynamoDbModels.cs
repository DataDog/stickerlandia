// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using OpenIddict.Abstractions;

namespace Stickerlandia.UserManagement.Auth.DynamoDB;

/// <summary>
/// DynamoDB representation of OpenIddict Application
/// </summary>
[DynamoDBTable("OpenIddictApplications")]
public class DynamoDbApplication
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;
    
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ConcurrencyToken { get; set; }
    public string? ConsentType { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNames { get; set; } // JSON serialized
    public string? Endpoints { get; set; } // JSON serialized
    public string? GrantTypes { get; set; } // JSON serialized
    public string? Permissions { get; set; } // JSON serialized
    public string? PostLogoutRedirectUris { get; set; } // JSON serialized
    public string? Properties { get; set; } // JSON serialized
    public string? RedirectUris { get; set; } // JSON serialized
    public string? Requirements { get; set; } // JSON serialized
    public string? ResponseTypes { get; set; } // JSON serialized
    public string? Type { get; set; }
}

/// <summary>
/// DynamoDB representation of OpenIddict Authorization
/// </summary>
[DynamoDBTable("OpenIddictAuthorizations")]
public class DynamoDbAuthorization
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;
    
    public string? ApplicationId { get; set; }
    public string? ConcurrencyToken { get; set; }
    public DateTime? CreationDate { get; set; }
    public string? Properties { get; set; } // JSON serialized
    public string? Scopes { get; set; } // JSON serialized
    public string? Status { get; set; }
    public string? Subject { get; set; }
    public string? Type { get; set; }
}

/// <summary>
/// DynamoDB representation of OpenIddict Scope
/// </summary>
[DynamoDBTable("OpenIddictScopes")]
public class DynamoDbScope
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;
    
    public string? ConcurrencyToken { get; set; }
    public string? Description { get; set; }
    public string? Descriptions { get; set; } // JSON serialized
    public string? DisplayName { get; set; }
    public string? DisplayNames { get; set; } // JSON serialized
    public string? Name { get; set; }
    public string? Properties { get; set; } // JSON serialized
    public string? Resources { get; set; } // JSON serialized
}

/// <summary>
/// DynamoDB representation of OpenIddict Token
/// </summary>
[DynamoDBTable("OpenIddictTokens")]
public class DynamoDbToken
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;
    
    public string? ApplicationId { get; set; }
    public string? AuthorizationId { get; set; }
    public string? ConcurrencyToken { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Payload { get; set; }
    public string? Properties { get; set; } // JSON serialized
    public DateTime? RedemptionDate { get; set; }
    public string? ReferenceId { get; set; }
    public string? Status { get; set; }
    public string? Subject { get; set; }
    public string? Type { get; set; }
} 