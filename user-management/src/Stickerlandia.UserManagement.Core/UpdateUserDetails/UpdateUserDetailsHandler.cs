// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.UserManagement.Core.UpdateUserDetails;

public class UpdateUserDetailsHandler(IUsers users)
{
    public async Task Handle(UpdateUserDetailsRequest command)
    {
        try
        {
            if (!command.IsValid())
            {
                throw new ArgumentException("Invalid UpdateUserDetailsRequest");
            }
            
            // Check if email exists before creating account
            var exisingAccount = await users.WithIdAsync(command.AccountId!);
            
            if (exisingAccount == null)
            {
                throw new InvalidUserException($"User with ID {command.AccountId} not found.");
            }
            
            exisingAccount.UpdateUserDetails(command.FirstName, command.LastName);

            if (!exisingAccount.Changed)
            {
                return;
            }
            
            await users.UpdateAccount(exisingAccount);
        }
        catch (UserExistsException ex)
        {
            Activity.Current?.AddTag("user.exists", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
        catch (Exception ex)
        {
            Activity.Current?.AddTag("user.registration.failed", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
}