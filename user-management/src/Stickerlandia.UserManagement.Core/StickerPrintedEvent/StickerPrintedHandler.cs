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

namespace Stickerlandia.UserManagement.Core.StickerPrintedEvent;

public class StickerPrintedHandler(UserManager<PostgresUserAccount> users)
{
    public async Task Handle(StickerPrintedEventV1 eventV1)
    {
        try
        {
            if (eventV1 == null || string.IsNullOrEmpty(eventV1.UserId))
            {
                throw new ArgumentException("Invalid StickerPrintedEventV1");
            }

            var account = await users.FindByIdAsync(eventV1.UserId);

            if (account is null)
            {
                throw new InvalidUserException("No user found with the provided account ID.");
            }
            
            account.StickerPrinted();

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