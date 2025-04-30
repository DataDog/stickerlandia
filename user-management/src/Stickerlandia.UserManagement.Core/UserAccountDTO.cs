// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Core;

public record UserAccountDTO
{
    public UserAccountDTO(UserAccount userAccount)
    {
        AccountId = userAccount.Id;
        EmailAddress = userAccount.EmailAddress;
        FirstName = userAccount.FirstName;
        LastName = userAccount.LastName;
        ClaimedStickerCount = userAccount.ClaimedStickerCount;
    }

    public string AccountId { get; set; }

    public string EmailAddress { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public int ClaimedStickerCount { get; set; }
}