// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.UserManagement.Core.Auth;

public class AccountDetailsHandler(IUsers users, IAuthService authService)
{
    public async Task<UserAccountDTO> GetAccountByAuthToken(string authToken)
    {
        try
        {
            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentException("Invalid auth token");
            }
            
            var authenticatedUser = authService.ValidateAuthToken(authToken);
            
            if (authenticatedUser is null)
            {
                throw new InvalidUserException("Invalid auth token");
            }

            var account = await users.WithIdAsync(authenticatedUser.AccountId!);

            if (account == null)
            {
                throw new InvalidUserException("User not found");
            }

            return new UserAccountDTO(account);
        }
        catch (InvalidUserException ex)
        {
            Activity.Current?.AddTag("user.notfound", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
} 