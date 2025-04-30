// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Core;

public interface IUserAccountRepository
{
    Task<UserAccount> CreateAccount(UserAccount userAccount);
    
    Task UpdateAccount(UserAccount userAccount);

    Task<UserAccount> ValidateCredentials(string emailAddress, string password);
    
    Task SeedInitialUser();
    
    // New methods for performance optimization
    Task<UserAccount?> GetAccountByIdAsync(string accountId);
    
    Task<UserAccount?> GetAccountByEmailAsync(string emailAddress);
    
    Task<bool> DoesEmailExistAsync(string emailAddress);
    
    Task PreloadFrequentUsersAsync();
    
    Task InvalidateCacheForUserAsync(string accountId);
}
