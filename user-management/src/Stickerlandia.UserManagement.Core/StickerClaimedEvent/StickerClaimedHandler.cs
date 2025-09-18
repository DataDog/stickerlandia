/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace Stickerlandia.UserManagement.Core.StickerClaimedEvent;

public class StickerClaimedHandler(UserManager<PostgresUserAccount> users)
{
    public async Task Handle(StickerClaimedEventV1 eventV1)
    {
        try
        {
            if (eventV1 == null || string.IsNullOrEmpty(eventV1.AccountId))
            {
                throw new ArgumentException("Invalid StickerClaimedEventV1");
            }

            var account = await users.FindByIdAsync(eventV1.AccountId);

            if (account is null)
            {
                throw new InvalidUserException("No user found with the provided account ID.");
            }
            
            account.StickerOrdered();

            await users.UpdateAsync(account);
        }
        catch (Exception ex)
        {
            Activity.Current?.AddTag("stickerClaim.failed", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
}