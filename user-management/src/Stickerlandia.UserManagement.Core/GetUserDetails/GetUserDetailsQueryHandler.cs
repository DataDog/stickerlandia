// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace Stickerlandia.UserManagement.Core.GetUserDetails;

public class GetUserDetailsQueryHandler(UserManager<PostgresUserAccount> userManager)
{
    public async Task<UserAccountDto> Handle(GetUserDetailsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        
        try
        {
            if (query.AccountId is null)
            {
                throw new ArgumentException("Invalid auth token");
            }

            var account = await userManager.FindByIdAsync(query.AccountId.Value);

            if (account == null)
            {
                throw new InvalidUserException("User not found");
            }

            return new UserAccountDto(account);
        }
        catch (InvalidUserException ex)
        {
            Activity.Current?.AddTag("user.notfound", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
}