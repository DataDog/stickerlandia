// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Security.Claims;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Identity;

namespace Stickerlandia.UserManagement.Auth.DynamoDB;

/// <summary>
/// DynamoDB implementation of IRoleStore for ASP.NET Core Identity
/// </summary>
public class DynamoDbRoleStore : 
    IRoleStore<IdentityRole>,
    IRoleClaimStore<IdentityRole>
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public DynamoDbRoleStore(DynamoDBContext context, IAmazonDynamoDB dynamoDb)
    {
        _context = context;
        _dynamoDb = dynamoDb;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region IRoleStore

    public async Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        var dynamoDbRole = DynamoDbRole.FromIdentityRole(role);
        await _context.SaveAsync(dynamoDbRole, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        await _context.DeleteAsync<DynamoDbRole>(role.Id, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        var dynamoDbRole = await _context.LoadAsync<DynamoDbRole>(roleId, cancellationToken);
        return dynamoDbRole?.ToIdentityRole();
    }

    public async Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        // Use scan to find role by normalized name - not optimal but functional
        var allRoles = await _context.ScanAsync<DynamoDbRole>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var role = allRoles.FirstOrDefault(r => r.NormalizedName == normalizedRoleName);
        return role?.ToIdentityRole();
    }

    public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.NormalizedName);
    }

    public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id);
    }

    public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(IdentityRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(IdentityRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        var dynamoDbRole = DynamoDbRole.FromIdentityRole(role);
        await _context.SaveAsync(dynamoDbRole, cancellationToken);
        return IdentityResult.Success;
    }

    #endregion

    #region IRoleClaimStore

    public async Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        var roleClaim = new IdentityRoleClaim<string>
        {
            RoleId = role.Id,
            ClaimType = claim.Type,
            ClaimValue = claim.Value
        };
        
        var dynamoDbRoleClaim = DynamoDbRoleClaim.FromIdentityRoleClaim(roleClaim);
        await _context.SaveAsync(dynamoDbRoleClaim, cancellationToken);
    }

    public async Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default)
    {
        var roleClaims = await _context.QueryAsync<DynamoDbRoleClaim>(role.Id)
            .GetRemainingAsync(cancellationToken);
        
        return roleClaims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!)).ToList();
    }

    public async Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        var existingClaims = await _context.QueryAsync<DynamoDbRoleClaim>(role.Id)
            .GetRemainingAsync(cancellationToken);
        
        var claimsToRemove = existingClaims.Where(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        
        foreach (var claimToRemove in claimsToRemove)
        {
            await _context.DeleteAsync(claimToRemove, cancellationToken);
        }
    }

    #endregion
} 