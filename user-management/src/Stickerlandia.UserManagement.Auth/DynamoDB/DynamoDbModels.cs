// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Identity;

namespace Stickerlandia.UserManagement.Auth.DynamoDB;

/// <summary>
/// DynamoDB representation of IdentityUser
/// </summary>
[DynamoDBTable("Users")]
public class DynamoDbUser
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;
    
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }

    public IdentityUser ToIdentityUser()
    {
        return new IdentityUser
        {
            Id = Id,
            UserName = UserName,
            NormalizedUserName = NormalizedUserName,
            Email = Email,
            NormalizedEmail = NormalizedEmail,
            EmailConfirmed = EmailConfirmed,
            PasswordHash = PasswordHash,
            SecurityStamp = SecurityStamp,
            ConcurrencyStamp = ConcurrencyStamp,
            PhoneNumber = PhoneNumber,
            PhoneNumberConfirmed = PhoneNumberConfirmed,
            TwoFactorEnabled = TwoFactorEnabled,
            LockoutEnd = LockoutEnd,
            LockoutEnabled = LockoutEnabled,
            AccessFailedCount = AccessFailedCount
        };
    }

    public static DynamoDbUser FromIdentityUser(IdentityUser user)
    {
        return new DynamoDbUser
        {
            Id = user.Id,
            UserName = user.UserName,
            NormalizedUserName = user.NormalizedUserName,
            Email = user.Email,
            NormalizedEmail = user.NormalizedEmail,
            EmailConfirmed = user.EmailConfirmed,
            PasswordHash = user.PasswordHash,
            SecurityStamp = user.SecurityStamp,
            ConcurrencyStamp = user.ConcurrencyStamp,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnd = user.LockoutEnd,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount
        };
    }
}

/// <summary>
/// DynamoDB representation of IdentityRole
/// </summary>
[DynamoDBTable("Roles")]
public class DynamoDbRole
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty;
    
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? ConcurrencyStamp { get; set; }

    public IdentityRole ToIdentityRole()
    {
        return new IdentityRole
        {
            Id = Id,
            Name = Name,
            NormalizedName = NormalizedName,
            ConcurrencyStamp = ConcurrencyStamp
        };
    }

    public static DynamoDbRole FromIdentityRole(IdentityRole role)
    {
        return new DynamoDbRole
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            ConcurrencyStamp = role.ConcurrencyStamp
        };
    }
}

/// <summary>
/// DynamoDB representation of IdentityUserClaim
/// </summary>
[DynamoDBTable("UserClaims")]
public class DynamoDbUserClaim
{
    [DynamoDBHashKey]
    public string UserId { get; set; } = string.Empty;
    
    [DynamoDBRangeKey]
    public string ClaimKey { get; set; } = string.Empty; // Combination of ClaimType and ClaimValue for uniqueness
    
    public int Id { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }

    public IdentityUserClaim<string> ToIdentityUserClaim()
    {
        return new IdentityUserClaim<string>
        {
            Id = Id,
            UserId = UserId,
            ClaimType = ClaimType,
            ClaimValue = ClaimValue
        };
    }

    public static DynamoDbUserClaim FromIdentityUserClaim(IdentityUserClaim<string> claim)
    {
        return new DynamoDbUserClaim
        {
            Id = claim.Id,
            UserId = claim.UserId,
            ClaimType = claim.ClaimType,
            ClaimValue = claim.ClaimValue,
            ClaimKey = $"{claim.ClaimType}#{claim.ClaimValue}#{claim.Id}"
        };
    }
}

/// <summary>
/// DynamoDB representation of IdentityUserLogin
/// </summary>
[DynamoDBTable("UserLogins")]
public class DynamoDbUserLogin
{
    [DynamoDBHashKey]
    public string UserId { get; set; } = string.Empty;
    
    [DynamoDBRangeKey]
    public string LoginKey { get; set; } = string.Empty; // LoginProvider#ProviderKey
    
    public string LoginProvider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }

    public IdentityUserLogin<string> ToIdentityUserLogin()
    {
        return new IdentityUserLogin<string>
        {
            UserId = UserId,
            LoginProvider = LoginProvider,
            ProviderKey = ProviderKey,
            ProviderDisplayName = ProviderDisplayName
        };
    }

    public static DynamoDbUserLogin FromIdentityUserLogin(IdentityUserLogin<string> login)
    {
        return new DynamoDbUserLogin
        {
            UserId = login.UserId,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName,
            LoginKey = $"{login.LoginProvider}#{login.ProviderKey}"
        };
    }
}

/// <summary>
/// DynamoDB representation of IdentityUserToken
/// </summary>
[DynamoDBTable("UserTokens")]
public class DynamoDbUserToken
{
    [DynamoDBHashKey]
    public string UserId { get; set; } = string.Empty;
    
    [DynamoDBRangeKey]
    public string TokenKey { get; set; } = string.Empty; // LoginProvider#Name
    
    public string LoginProvider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }

    public IdentityUserToken<string> ToIdentityUserToken()
    {
        return new IdentityUserToken<string>
        {
            UserId = UserId,
            LoginProvider = LoginProvider,
            Name = Name,
            Value = Value
        };
    }

    public static DynamoDbUserToken FromIdentityUserToken(IdentityUserToken<string> token)
    {
        return new DynamoDbUserToken
        {
            UserId = token.UserId,
            LoginProvider = token.LoginProvider,
            Name = token.Name,
            Value = token.Value,
            TokenKey = $"{token.LoginProvider}#{token.Name}"
        };
    }
}

/// <summary>
/// DynamoDB representation of IdentityUserRole
/// </summary>
[DynamoDBTable("UserRoles")]
public class DynamoDbUserRole
{
    [DynamoDBHashKey]
    public string UserId { get; set; } = string.Empty;
    
    [DynamoDBRangeKey]
    public string RoleId { get; set; } = string.Empty;

    public IdentityUserRole<string> ToIdentityUserRole()
    {
        return new IdentityUserRole<string>
        {
            UserId = UserId,
            RoleId = RoleId
        };
    }

    public static DynamoDbUserRole FromIdentityUserRole(IdentityUserRole<string> userRole)
    {
        return new DynamoDbUserRole
        {
            UserId = userRole.UserId,
            RoleId = userRole.RoleId
        };
    }
}

/// <summary>
/// DynamoDB representation of IdentityRoleClaim
/// </summary>
[DynamoDBTable("RoleClaims")]
public class DynamoDbRoleClaim
{
    [DynamoDBHashKey]
    public string RoleId { get; set; } = string.Empty;
    
    [DynamoDBRangeKey]
    public string ClaimKey { get; set; } = string.Empty; // Combination of ClaimType and ClaimValue for uniqueness
    
    public int Id { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }

    public IdentityRoleClaim<string> ToIdentityRoleClaim()
    {
        return new IdentityRoleClaim<string>
        {
            Id = Id,
            RoleId = RoleId,
            ClaimType = ClaimType,
            ClaimValue = ClaimValue
        };
    }

    public static DynamoDbRoleClaim FromIdentityRoleClaim(IdentityRoleClaim<string> claim)
    {
        return new DynamoDbRoleClaim
        {
            Id = claim.Id,
            RoleId = claim.RoleId,
            ClaimType = claim.ClaimType,
            ClaimValue = claim.ClaimValue,
            ClaimKey = $"{claim.ClaimType}#{claim.ClaimValue}#{claim.Id}"
        };
    }
} 