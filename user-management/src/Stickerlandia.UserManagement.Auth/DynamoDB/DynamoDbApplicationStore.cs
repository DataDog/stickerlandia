// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace Stickerlandia.UserManagement.Auth.DynamoDB;

/// <summary>
/// DynamoDB implementation of IOpenIddictApplicationStore
/// </summary>
public class DynamoDbApplicationStore : IOpenIddictApplicationStore<DynamoDbApplication>
{
    private readonly DynamoDBContext _context;

    public DynamoDbApplicationStore(DynamoDBContext context)
    {
        _context = context;
    }

    public ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        // Note: This is not efficient for DynamoDB, but required by the interface
        return new ValueTask<long>(0L);
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<DynamoDbApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        return new ValueTask<long>(0L);
    }

    public async ValueTask<DynamoDbApplication> CreateAsync(OpenIddictApplicationDescriptor descriptor, CancellationToken cancellationToken)
    {
        var application = new DynamoDbApplication
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = descriptor.ClientId,
            ClientSecret = descriptor.ClientSecret,
            ConsentType = descriptor.ConsentType,
            DisplayName = descriptor.DisplayName,
            ConcurrencyToken = Guid.NewGuid().ToString()
        };

        if (descriptor.Permissions.Count > 0)
        {
            application.Permissions = JsonSerializer.Serialize(descriptor.Permissions.ToArray());
        }

        await _context.SaveAsync(application, cancellationToken);
        return application;
    }

    public async ValueTask CreateAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.Id))
        {
            application.Id = Guid.NewGuid().ToString();
        }
        
        if (string.IsNullOrEmpty(application.ConcurrencyToken))
        {
            application.ConcurrencyToken = Guid.NewGuid().ToString();
        }

        await _context.SaveAsync(application, cancellationToken);
    }

    public async ValueTask DeleteAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        await _context.DeleteAsync(application, cancellationToken);
    }

    public async ValueTask<DynamoDbApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
    {
        var allApplications = await _context.ScanAsync<DynamoDbApplication>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        return allApplications.FirstOrDefault(a => a.ClientId == identifier);
    }

    public async ValueTask<DynamoDbApplication?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        return await _context.LoadAsync<DynamoDbApplication>(identifier, cancellationToken);
    }

    public async IAsyncEnumerable<DynamoDbApplication> FindByPostLogoutRedirectUriAsync(string uri, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allApplications = await _context.ScanAsync<DynamoDbApplication>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var matchingApps = allApplications.Where(a => 
            !string.IsNullOrEmpty(a.PostLogoutRedirectUris) && 
            a.PostLogoutRedirectUris.Contains(uri));
        
        foreach (var app in matchingApps)
        {
            yield return app;
        }
    }

    public async IAsyncEnumerable<DynamoDbApplication> FindByRedirectUriAsync(string uri, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allApplications = await _context.ScanAsync<DynamoDbApplication>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var matchingApps = allApplications.Where(a => 
            !string.IsNullOrEmpty(a.RedirectUris) && 
            a.RedirectUris.Contains(uri));
        
        foreach (var app in matchingApps)
        {
            yield return app;
        }
    }

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<DynamoDbApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("LINQ queries are not supported with DynamoDB store");
    }

    public ValueTask<string?> GetApplicationTypeAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(application.Type);
    }

    public ValueTask<string?> GetClientIdAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(application.ClientId);
    }

    public ValueTask<string?> GetClientSecretAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(application.ClientSecret);
    }

    public ValueTask<string?> GetClientTypeAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(application.Type);
    }

    public ValueTask<string?> GetConsentTypeAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(application.ConsentType);
    }

    public ValueTask<string?> GetDisplayNameAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(application.DisplayName);
    }

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.DisplayNames))
        {
            return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary<CultureInfo, string>.Empty);
        }

        try
        {
            var names = JsonSerializer.Deserialize<Dictionary<string, string>>(application.DisplayNames);
            if (names != null)
            {
                var cultureNames = ImmutableDictionary.CreateBuilder<CultureInfo, string>();
                foreach (var kvp in names)
                {
                    if (CultureInfo.GetCultureInfo(kvp.Key) is var culture)
                    {
                        cultureNames[culture] = kvp.Value;
                    }
                }
                return new ValueTask<ImmutableDictionary<CultureInfo, string>>(cultureNames.ToImmutable());
            }
        }
        catch (JsonException)
        {
            // Ignore deserialization errors
        }
        catch (CultureNotFoundException)
        {
            // Ignore invalid culture codes
        }
        
        return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary<CultureInfo, string>.Empty);
    }

    public ValueTask<string?> GetIdAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(application.Id);
    }

    public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        // DynamoDB implementation doesn't store JsonWebKeySet directly
        return new ValueTask<JsonWebKeySet?>(default(JsonWebKeySet));
    }

    public ValueTask<ImmutableArray<string>> GetPermissionsAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.Permissions))
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }

        try
        {
            var permissions = JsonSerializer.Deserialize<string[]>(application.Permissions);
            return new ValueTask<ImmutableArray<string>>(permissions?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }
    }

    public ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.PostLogoutRedirectUris))
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }

        try
        {
            var uris = JsonSerializer.Deserialize<string[]>(application.PostLogoutRedirectUris);
            return new ValueTask<ImmutableArray<string>>(uris?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.Properties))
        {
            return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary<string, JsonElement>.Empty);
        }

        try
        {
            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(application.Properties);
            return new ValueTask<ImmutableDictionary<string, JsonElement>>(
                properties?.ToImmutableDictionary() ?? ImmutableDictionary<string, JsonElement>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary<string, JsonElement>.Empty);
        }
    }

    public ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.RedirectUris))
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }

        try
        {
            var uris = JsonSerializer.Deserialize<string[]>(application.RedirectUris);
            return new ValueTask<ImmutableArray<string>>(uris?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }
    }

    public ValueTask<ImmutableArray<string>> GetRequirementsAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.Requirements))
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }

        try
        {
            var requirements = JsonSerializer.Deserialize<string[]>(application.Requirements);
            return new ValueTask<ImmutableArray<string>>(requirements?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }
    }

    public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(application.Properties))
        {
            return new ValueTask<ImmutableDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);
        }

        try
        {
            var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(application.Properties);
            return new ValueTask<ImmutableDictionary<string, string>>(
                settings?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);
        }
    }

    public ValueTask<DynamoDbApplication> InstantiateAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<DynamoDbApplication>(new DynamoDbApplication());
    }

    public async IAsyncEnumerable<DynamoDbApplication> ListAsync(int? count, int? offset, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allApplications = await _context.ScanAsync<DynamoDbApplication>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var applications = allApplications.AsEnumerable();
        
        if (offset.HasValue)
        {
            applications = applications.Skip(offset.Value);
        }
        
        if (count.HasValue)
        {
            applications = applications.Take(count.Value);
        }
        
        foreach (var application in applications)
        {
            yield return application;
        }
    }

    public async IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<DynamoDbApplication>, TState, IQueryable<TResult>> query, TState state, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        throw new NotSupportedException("LINQ queries are not supported with DynamoDB store");
        yield break; // Required for async enumerable
    }

    public async ValueTask UpdateAsync(DynamoDbApplication application, CancellationToken cancellationToken)
    {
        application.ConcurrencyToken = Guid.NewGuid().ToString();
        await _context.SaveAsync(application, cancellationToken);
    }

    public ValueTask SetApplicationTypeAsync(DynamoDbApplication application, string? type, CancellationToken cancellationToken)
    {
        application.Type = type;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetClientIdAsync(DynamoDbApplication application, string? identifier, CancellationToken cancellationToken)
    {
        application.ClientId = identifier;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetClientSecretAsync(DynamoDbApplication application, string? secret, CancellationToken cancellationToken)
    {
        application.ClientSecret = secret;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetClientTypeAsync(DynamoDbApplication application, string? type, CancellationToken cancellationToken)
    {
        application.Type = type;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetConsentTypeAsync(DynamoDbApplication application, string? type, CancellationToken cancellationToken)
    {
        application.ConsentType = type;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNameAsync(DynamoDbApplication application, string? name, CancellationToken cancellationToken)
    {
        application.DisplayName = name;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNamesAsync(DynamoDbApplication application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
    {
        var nameDict = names.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
        application.DisplayNames = JsonSerializer.Serialize(nameDict);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetJsonWebKeySetAsync(DynamoDbApplication application, JsonWebKeySet? jwks, CancellationToken cancellationToken)
    {
        // DynamoDB implementation doesn't store JsonWebKeySet directly
        // Could be serialized to Properties if needed
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPermissionsAsync(DynamoDbApplication application, ImmutableArray<string> permissions, CancellationToken cancellationToken)
    {
        application.Permissions = JsonSerializer.Serialize(permissions.ToArray());
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPostLogoutRedirectUrisAsync(DynamoDbApplication application, ImmutableArray<string> uris, CancellationToken cancellationToken)
    {
        application.PostLogoutRedirectUris = JsonSerializer.Serialize(uris.ToArray());
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPropertiesAsync(DynamoDbApplication application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        application.Properties = JsonSerializer.Serialize(properties);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetRedirectUrisAsync(DynamoDbApplication application, ImmutableArray<string> uris, CancellationToken cancellationToken)
    {
        application.RedirectUris = JsonSerializer.Serialize(uris.ToArray());
        return ValueTask.CompletedTask;
    }

    public ValueTask SetRequirementsAsync(DynamoDbApplication application, ImmutableArray<string> requirements, CancellationToken cancellationToken)
    {
        application.Requirements = JsonSerializer.Serialize(requirements.ToArray());
        return ValueTask.CompletedTask;
    }

    public ValueTask SetSettingsAsync(DynamoDbApplication application, ImmutableDictionary<string, string> settings, CancellationToken cancellationToken)
    {
        application.Properties = JsonSerializer.Serialize(settings);
        return ValueTask.CompletedTask;
    }
} 