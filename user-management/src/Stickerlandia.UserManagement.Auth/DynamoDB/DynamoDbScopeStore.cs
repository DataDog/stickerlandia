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
/// DynamoDB implementation of IOpenIddictScopeStore
/// </summary>
public class DynamoDbScopeStore : IOpenIddictScopeStore<DynamoDbScope>
{
    private readonly DynamoDBContext _context;

    public DynamoDbScopeStore(DynamoDBContext context)
    {
        _context = context;
    }

    public ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<long>(0L);
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<DynamoDbScope>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        return new ValueTask<long>(0L);
    }

    public async ValueTask<DynamoDbScope> CreateAsync(OpenIddictScopeDescriptor descriptor, CancellationToken cancellationToken)
    {
        var scope = new DynamoDbScope
        {
            Id = Guid.NewGuid().ToString(),
            Name = descriptor.Name,
            DisplayName = descriptor.DisplayName,
            Description = descriptor.Description,
            ConcurrencyToken = Guid.NewGuid().ToString()
        };

        if (descriptor.Resources.Count > 0)
        {
            scope.Resources = JsonSerializer.Serialize(descriptor.Resources.ToArray());
        }

        await _context.SaveAsync(scope, cancellationToken);
        return scope;
    }

    public async ValueTask CreateAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(scope.Id))
        {
            scope.Id = Guid.NewGuid().ToString();
        }
        
        if (string.IsNullOrEmpty(scope.ConcurrencyToken))
        {
            scope.ConcurrencyToken = Guid.NewGuid().ToString();
        }

        await _context.SaveAsync(scope, cancellationToken);
    }

    public async ValueTask DeleteAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        await _context.DeleteAsync(scope, cancellationToken);
    }

    public async ValueTask<DynamoDbScope?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        return await _context.LoadAsync<DynamoDbScope>(identifier, cancellationToken);
    }

    public async ValueTask<DynamoDbScope?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        var allScopes = await _context.ScanAsync<DynamoDbScope>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        return allScopes.FirstOrDefault(s => s.Name == name);
    }

    public async IAsyncEnumerable<DynamoDbScope> FindByNamesAsync(ImmutableArray<string> names, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allScopes = await _context.ScanAsync<DynamoDbScope>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var matchingScopes = allScopes.Where(s => names.Contains(s.Name));
        foreach (var scope in matchingScopes)
        {
            yield return scope;
        }
    }

    public async IAsyncEnumerable<DynamoDbScope> FindByResourceAsync(string resource, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allScopes = await _context.ScanAsync<DynamoDbScope>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var matchingScopes = allScopes.Where(s => 
            !string.IsNullOrEmpty(s.Resources) && 
            s.Resources.Contains(resource));
        
        foreach (var scope in matchingScopes)
        {
            yield return scope;
        }
    }

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<DynamoDbScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("LINQ queries are not supported with DynamoDB store");
    }

    public ValueTask<string?> GetDescriptionAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(scope.Description);
    }

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(scope.Descriptions))
        {
            return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary<CultureInfo, string>.Empty);
        }

        try
        {
            var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(scope.Descriptions);
            if (descriptions != null)
            {
                var cultureDescriptions = ImmutableDictionary.CreateBuilder<CultureInfo, string>();
                foreach (var kvp in descriptions)
                {
                    if (CultureInfo.GetCultureInfo(kvp.Key) is var culture)
                    {
                        cultureDescriptions[culture] = kvp.Value;
                    }
                }
                return new ValueTask<ImmutableDictionary<CultureInfo, string>>(cultureDescriptions.ToImmutable());
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

    public ValueTask<string?> GetDisplayNameAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(scope.DisplayName);
    }

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(scope.DisplayNames))
        {
            return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary<CultureInfo, string>.Empty);
        }

        try
        {
            var displayNames = JsonSerializer.Deserialize<Dictionary<string, string>>(scope.DisplayNames);
            if (displayNames != null)
            {
                var cultureDisplayNames = ImmutableDictionary.CreateBuilder<CultureInfo, string>();
                foreach (var kvp in displayNames)
                {
                    if (CultureInfo.GetCultureInfo(kvp.Key) is var culture)
                    {
                        cultureDisplayNames[culture] = kvp.Value;
                    }
                }
                return new ValueTask<ImmutableDictionary<CultureInfo, string>>(cultureDisplayNames.ToImmutable());
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

    public ValueTask<string?> GetIdAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(scope.Id);
    }

    public ValueTask<string?> GetNameAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        return new ValueTask<string?>(scope.Name);
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(scope.Properties))
        {
            return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary<string, JsonElement>.Empty);
        }

        try
        {
            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(scope.Properties);
            return new ValueTask<ImmutableDictionary<string, JsonElement>>(
                properties?.ToImmutableDictionary() ?? ImmutableDictionary<string, JsonElement>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary<string, JsonElement>.Empty);
        }
    }

    public ValueTask<ImmutableArray<string>> GetResourcesAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(scope.Resources))
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }

        try
        {
            var resources = JsonSerializer.Deserialize<string[]>(scope.Resources);
            return new ValueTask<ImmutableArray<string>>(resources?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
        }
        catch (JsonException)
        {
            return new ValueTask<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        }
    }

    public ValueTask<DynamoDbScope> InstantiateAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<DynamoDbScope>(new DynamoDbScope());
    }

    public async IAsyncEnumerable<DynamoDbScope> ListAsync(int? count, int? offset, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allScopes = await _context.ScanAsync<DynamoDbScope>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var scopes = allScopes.AsEnumerable();
        
        if (offset.HasValue)
        {
            scopes = scopes.Skip(offset.Value);
        }
        
        if (count.HasValue)
        {
            scopes = scopes.Take(count.Value);
        }
        
        foreach (var scope in scopes)
        {
            yield return scope;
        }
    }

    public async IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<DynamoDbScope>, TState, IQueryable<TResult>> query, TState state, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        throw new NotSupportedException("LINQ queries are not supported with DynamoDB store");
        yield break; // Required for async enumerable
    }

    public async ValueTask UpdateAsync(DynamoDbScope scope, CancellationToken cancellationToken)
    {
        scope.ConcurrencyToken = Guid.NewGuid().ToString();
        await _context.SaveAsync(scope, cancellationToken);
    }

    public ValueTask SetDescriptionAsync(DynamoDbScope scope, string? description, CancellationToken cancellationToken)
    {
        scope.Description = description;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDescriptionsAsync(DynamoDbScope scope, ImmutableDictionary<CultureInfo, string> descriptions, CancellationToken cancellationToken)
    {
        var descriptionDict = descriptions.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
        scope.Descriptions = JsonSerializer.Serialize(descriptionDict);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNameAsync(DynamoDbScope scope, string? name, CancellationToken cancellationToken)
    {
        scope.DisplayName = name;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetDisplayNamesAsync(DynamoDbScope scope, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
    {
        var nameDict = names.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
        scope.DisplayNames = JsonSerializer.Serialize(nameDict);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetNameAsync(DynamoDbScope scope, string? name, CancellationToken cancellationToken)
    {
        scope.Name = name;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPropertiesAsync(DynamoDbScope scope, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        scope.Properties = JsonSerializer.Serialize(properties);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetResourcesAsync(DynamoDbScope scope, ImmutableArray<string> resources, CancellationToken cancellationToken)
    {
        scope.Resources = JsonSerializer.Serialize(resources.ToArray());
        return ValueTask.CompletedTask;
    }
} 