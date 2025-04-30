// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.UserManagement.Core.StickerClaimedEvent;

public class StickerClaimedEventHandler(IUserAccountRepository userAccountRepository)
{
    public async Task Handle(StickerClaimedEventV1 eventV1)
    {
        try
        {
            if (eventV1 == null || string.IsNullOrEmpty(eventV1.AccountId))
            {
                throw new ArgumentException("Invalid StickerClaimedEventV1");
            }

            var account = await userAccountRepository.GetAccountByIdAsync(eventV1.AccountId);

            if (account is null)
            {
                return;
            }
            
            account.StickerOrdered(eventV1.StickerId);

            await userAccountRepository.UpdateAccount(account);
        }
        catch (Exception ex)
        {
            Activity.Current?.AddTag("stickerClaim.failed", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
}