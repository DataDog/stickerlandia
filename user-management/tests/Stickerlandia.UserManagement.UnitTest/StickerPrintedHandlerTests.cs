/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;

namespace Stickerlandia.UserManagement.UnitTest;

public class StickerPrintedHandlerTests
{
    [Fact]
    public async Task GivenNullEvent_ShouldThrowArgumentException()
    {
        using var userManager = CreateFakeUserManager();
        var handler = new StickerPrintedHandler(userManager);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(null!));
    }

    [Fact]
    public async Task GivenEventWithEmptyUserId_ShouldThrowArgumentException()
    {
        using var userManager = CreateFakeUserManager();
        var handler = new StickerPrintedHandler(userManager);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new StickerPrintedEventV1 { UserId = "" }));
    }

    [Fact]
    public async Task GivenUserDoesNotExist_ShouldThrowInvalidUserException()
    {
        using var userManager = CreateFakeUserManager();
        A.CallTo(() => userManager.FindByIdAsync(A<string>.Ignored))
            .Returns(Task.FromResult<PostgresUserAccount?>(null));

        var handler = new StickerPrintedHandler(userManager);

        await Assert.ThrowsAsync<InvalidUserException>(() =>
            handler.Handle(new StickerPrintedEventV1 { UserId = "nonexistent-id" }));
    }

    [Fact]
    public async Task GivenValidEvent_ShouldIncrementPrintedStickerCount()
    {
        var userId = "valid-user-id";
        var account = new PostgresUserAccount { Id = userId };
        var initialCount = account.PrintedStickerCount;

        PostgresUserAccount? updatedAccount = null;

        using var userManager = CreateFakeUserManager();
        A.CallTo(() => userManager.FindByIdAsync(userId))
            .Returns(Task.FromResult<PostgresUserAccount?>(account));
        A.CallTo(() => userManager.UpdateAsync(A<PostgresUserAccount>.Ignored))
            .Invokes((PostgresUserAccount a) => updatedAccount = a)
            .Returns(Task.FromResult(IdentityResult.Success));

        var handler = new StickerPrintedHandler(userManager);

        await handler.Handle(new StickerPrintedEventV1 { UserId = userId });

        updatedAccount.Should().NotBeNull();
        updatedAccount!.PrintedStickerCount.Should().Be(initialCount + 1);
    }

    private static UserManager<PostgresUserAccount> CreateFakeUserManager()
    {
        var userManager = A.Fake<UserManager<PostgresUserAccount>>(options =>
            options.WithArgumentsForConstructor(() => new UserManager<PostgresUserAccount>(
                A.Fake<IUserStore<PostgresUserAccount>>(),
                A.Fake<IOptions<IdentityOptions>>(),
                A.Fake<IPasswordHasher<PostgresUserAccount>>(),
                new List<IUserValidator<PostgresUserAccount>>(),
                new List<IPasswordValidator<PostgresUserAccount>>(),
                A.Fake<ILookupNormalizer>(),
                A.Fake<IdentityErrorDescriber>(),
                CreateFakeServiceProvider(),
                A.Fake<ILogger<UserManager<PostgresUserAccount>>>()
            )));

        return userManager;
    }

    private static IServiceProvider CreateFakeServiceProvider()
    {
        var serviceProvider = A.Fake<IServiceProvider>();
        A.CallTo(() => serviceProvider.GetService(typeof(IMeterFactory)))
            .Returns(null);
        return serviceProvider;
    }
}
