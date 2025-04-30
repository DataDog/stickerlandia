// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.UserManagement.Core.Auth;

namespace Stickerlandia.UserManagement.Core.GetUserDetails;

public class GetUserDetailsQueryHandler(IUserAccountRepository userAccountRepository, IAuthService authService)
{
    public async Task<UserAccountDTO> Handle(GetUserDetailsQuery query)
    {
        try
        {
            if (string.IsNullOrEmpty(query.AuthHeader))
            {
                throw new ArgumentException("Invalid auth token");
            }
            
            var authenticatedUser = authService.ValidateAuthToken(query.AuthHeader);
            
            if (authenticatedUser is null)
            {
                throw new InvalidUserException("Invalid auth token");
            }

            var account = await userAccountRepository.GetAccountByIdAsync(authenticatedUser.AccountId);

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