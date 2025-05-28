// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Security.Claims;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Identity;

namespace Stickerlandia.UserManagement.Auth.DynamoDB;

/// <summary>
/// DynamoDB implementation of IUserStore for ASP.NET Core Identity
/// </summary>
public class DynamoDbUserStore : 
    IUserStore<IdentityUser>,
    IUserEmailStore<IdentityUser>,
    IUserPasswordStore<IdentityUser>,
    IUserSecurityStampStore<IdentityUser>,
    IUserClaimStore<IdentityUser>,
    IUserLoginStore<IdentityUser>,
    IUserRoleStore<IdentityUser>,
    IUserLockoutStore<IdentityUser>,
    IUserPhoneNumberStore<IdentityUser>,
    IUserTwoFactorStore<IdentityUser>,
    IUserAuthenticatorKeyStore<IdentityUser>,
    IUserTwoFactorRecoveryCodeStore<IdentityUser>,
    IDisposable
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public DynamoDbUserStore(DynamoDBContext context, IAmazonDynamoDB dynamoDb)
    {
        _context = context;
        _dynamoDb = dynamoDb;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region IUserStore

    public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var dynamoDbUser = DynamoDbUser.FromIdentityUser(user);
        await _context.SaveAsync(dynamoDbUser, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await _context.DeleteAsync<DynamoDbUser>(user.Id, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var dynamoDbUser = await _context.LoadAsync<DynamoDbUser>(userId, cancellationToken);
        return dynamoDbUser?.ToIdentityUser();
    }

    public async Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        // Use scan to find user by normalized username - not optimal but functional
        var allUsers = await _context.ScanAsync<DynamoDbUser>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var user = allUsers.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName);
        return user?.ToIdentityUser();
    }

    public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var dynamoDbUser = DynamoDbUser.FromIdentityUser(user);
        await _context.SaveAsync(dynamoDbUser, cancellationToken);
        return IdentityResult.Success;
    }

    #endregion

    #region IUserEmailStore

    public async Task<IdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        // Use scan to find user by normalized email - not optimal but functional
        var allUsers = await _context.ScanAsync<DynamoDbUser>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var user = allUsers.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail);
        return user?.ToIdentityUser();
    }

    public Task<string?> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task<string?> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetEmailAsync(IdentityUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(IdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPasswordStore

    public Task<string?> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    public Task SetPasswordHashAsync(IdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserSecurityStampStore

    public Task<string?> GetSecurityStampAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.SecurityStamp);
    }

    public Task SetSecurityStampAsync(IdentityUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserClaimStore

    public async Task AddClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var userClaim = new IdentityUserClaim<string>
            {
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            };
            
            var dynamoDbUserClaim = DynamoDbUserClaim.FromIdentityUserClaim(userClaim);
            await _context.SaveAsync(dynamoDbUserClaim, cancellationToken);
        }
    }

    public async Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var userClaims = await _context.QueryAsync<DynamoDbUserClaim>(user.Id)
            .GetRemainingAsync(cancellationToken);
        
        return userClaims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!)).ToList();
    }

    public async Task<IList<IdentityUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        // Scan all user claims - not optimal but functional
        var allUserClaims = await _context.ScanAsync<DynamoDbUserClaim>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var matchingClaims = allUserClaims.Where(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        var userIds = matchingClaims.Select(c => c.UserId).Distinct();
        
        var users = new List<IdentityUser>();
        foreach (var userId in userIds)
        {
            var user = await FindByIdAsync(userId, cancellationToken);
            if (user != null)
                users.Add(user);
        }
        
        return users;
    }

    public async Task RemoveClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var existingClaims = await _context.QueryAsync<DynamoDbUserClaim>(user.Id)
                .GetRemainingAsync(cancellationToken);
            
            var claimsToRemove = existingClaims.Where(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
            
            foreach (var claimToRemove in claimsToRemove)
            {
                await _context.DeleteAsync(claimToRemove, cancellationToken);
            }
        }
    }

    public async Task ReplaceClaimAsync(IdentityUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        await RemoveClaimsAsync(user, new[] { claim }, cancellationToken);
        await AddClaimsAsync(user, new[] { newClaim }, cancellationToken);
    }

    #endregion

    #region IUserLoginStore

    public async Task AddLoginAsync(IdentityUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        var userLogin = new IdentityUserLogin<string>
        {
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName
        };
        
        var dynamoDbUserLogin = DynamoDbUserLogin.FromIdentityUserLogin(userLogin);
        await _context.SaveAsync(dynamoDbUserLogin, cancellationToken);
    }

    public async Task<IdentityUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        // Scan all user logins - not optimal but functional
        var allUserLogins = await _context.ScanAsync<DynamoDbUserLogin>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var userLogin = allUserLogins.FirstOrDefault(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
        
        if (userLogin != null)
        {
            return await FindByIdAsync(userLogin.UserId, cancellationToken);
        }
        
        return null;
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var userLogins = await _context.QueryAsync<DynamoDbUserLogin>(user.Id)
            .GetRemainingAsync(cancellationToken);
        
        return userLogins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
            .ToList();
    }

    public async Task RemoveLoginAsync(IdentityUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var loginKey = $"{loginProvider}#{providerKey}";
        await _context.DeleteAsync<DynamoDbUserLogin>(user.Id, loginKey, cancellationToken);
    }

    #endregion

    #region IUserRoleStore

    public async Task AddToRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        // First find the role by name to get its ID
        var roleStore = new DynamoDbRoleStore(_context, _dynamoDb);
        var role = await roleStore.FindByNameAsync(roleName, cancellationToken);
        
        if (role != null)
        {
            var userRole = new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            
            var dynamoDbUserRole = DynamoDbUserRole.FromIdentityUserRole(userRole);
            await _context.SaveAsync(dynamoDbUserRole, cancellationToken);
        }
    }

    public async Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var userRoles = await _context.QueryAsync<DynamoDbUserRole>(user.Id)
            .GetRemainingAsync(cancellationToken);
        
        var roleStore = new DynamoDbRoleStore(_context, _dynamoDb);
        var roleNames = new List<string>();
        
        foreach (var userRole in userRoles)
        {
            var role = await roleStore.FindByIdAsync(userRole.RoleId, cancellationToken);
            if (role?.Name != null)
            {
                roleNames.Add(role.Name);
            }
        }
        
        return roleNames;
    }

    public async Task<IList<IdentityUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var roleStore = new DynamoDbRoleStore(_context, _dynamoDb);
        var role = await roleStore.FindByNameAsync(roleName, cancellationToken);
        
        if (role == null)
            return new List<IdentityUser>();
        
        // Scan all user roles to find users in this role
        var allUserRoles = await _context.ScanAsync<DynamoDbUserRole>(new List<ScanCondition>())
            .GetRemainingAsync(cancellationToken);
        
        var userRolesForRole = allUserRoles.Where(ur => ur.RoleId == role.Id);
        
        var users = new List<IdentityUser>();
        foreach (var userRole in userRolesForRole)
        {
            var user = await FindByIdAsync(userRole.UserId, cancellationToken);
            if (user != null)
                users.Add(user);
        }
        
        return users;
    }

    public async Task<bool> IsInRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync(user, cancellationToken);
        return roles.Contains(roleName);
    }

    public async Task RemoveFromRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var roleStore = new DynamoDbRoleStore(_context, _dynamoDb);
        var role = await roleStore.FindByNameAsync(roleName, cancellationToken);
        
        if (role != null)
        {
            await _context.DeleteAsync<DynamoDbUserRole>(user.Id, role.Id, cancellationToken);
        }
    }

    #endregion

    #region IUserLockoutStore

    public Task<int> GetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<bool> GetLockoutEnabledAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public Task<int> IncrementAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task SetLockoutEnabledAsync(IdentityUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPhoneNumberStore

    public Task<string?> GetPhoneNumberAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberAsync(IdentityUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task SetPhoneNumberConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserTwoFactorStore

    public Task<bool> GetTwoFactorEnabledAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.TwoFactorEnabled);
    }

    public Task SetTwoFactorEnabledAsync(IdentityUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserAuthenticatorKeyStore

    public async Task<string?> GetAuthenticatorKeyAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var tokenKey = $"[AspNetUserStore]#AuthenticatorKey";
        var token = await _context.LoadAsync<DynamoDbUserToken>(user.Id, tokenKey, cancellationToken);
        return token?.Value;
    }

    public async Task SetAuthenticatorKeyAsync(IdentityUser user, string key, CancellationToken cancellationToken)
    {
        var token = new DynamoDbUserToken
        {
            UserId = user.Id,
            LoginProvider = "[AspNetUserStore]",
            Name = "AuthenticatorKey",
            Value = key,
            TokenKey = $"[AspNetUserStore]#AuthenticatorKey"
        };
        
        await _context.SaveAsync(token, cancellationToken);
    }

    #endregion

    #region IUserTwoFactorRecoveryCodeStore

    public async Task<int> CountCodesAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var tokenKey = $"[AspNetUserStore]#RecoveryCodes";
        var token = await _context.LoadAsync<DynamoDbUserToken>(user.Id, tokenKey, cancellationToken);
        if (token?.Value != null)
        {
            return token.Value.Split(';').Length;
        }
        return 0;
    }

    public async Task<bool> RedeemCodeAsync(IdentityUser user, string code, CancellationToken cancellationToken)
    {
        var tokenKey = $"[AspNetUserStore]#RecoveryCodes";
        var token = await _context.LoadAsync<DynamoDbUserToken>(user.Id, tokenKey, cancellationToken);
        if (token?.Value != null)
        {
            var codes = token.Value.Split(';').ToList();
            if (codes.Remove(code))
            {
                token.Value = string.Join(";", codes);
                await _context.SaveAsync(token, cancellationToken);
                return true;
            }
        }
        return false;
    }

    public async Task ReplaceCodesAsync(IdentityUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var token = new DynamoDbUserToken
        {
            UserId = user.Id,
            LoginProvider = "[AspNetUserStore]",
            Name = "RecoveryCodes",
            Value = string.Join(";", recoveryCodes),
            TokenKey = $"[AspNetUserStore]#RecoveryCodes"
        };
        
        await _context.SaveAsync(token, cancellationToken);
    }

    #endregion
} 