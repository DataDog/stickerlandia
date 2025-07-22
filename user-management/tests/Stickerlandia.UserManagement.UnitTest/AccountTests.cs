using Microsoft.AspNetCore.Identity;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Stickerlandia.UserManagement.Core.UpdateUserDetails;

namespace Stickerlandia.UserManagement.UnitTest;

public class AccountTests
{
    [Fact]
    public async Task GivenAUserHasValidDetailsShouldRegisterSuccessfully()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";

        PostgresUserAccount? capturedAccount = null;
        DomainEvent? storedEvent = null;

        var userManager = A.Fake<UserManager<PostgresUserAccount>>();
        A.CallTo(() => userManager.FindByEmailAsync(A<string>.Ignored))
            .Returns(Task.FromResult<PostgresUserAccount?>(null));
        A.CallTo(() => userManager.CreateAsync(A<PostgresUserAccount>.Ignored, A<string>.Ignored))
            .Invokes((PostgresUserAccount account, string password) => capturedAccount = account)
            .Returns(Task.FromResult(IdentityResult.Success));
        A.CallTo(() => userManager.GetUserIdAsync(A<PostgresUserAccount>.Ignored))
            .ReturnsLazily(() => Task.FromResult(capturedAccount!.Id));

        var userStore = A.Fake<IUserStore<PostgresUserAccount>>();

        var outbox = A.Fake<IOutbox>();
        A.CallTo(() => outbox.StoreEventFor(A<string>.Ignored, A<DomainEvent>.Ignored))
            .Invokes((string accountId, DomainEvent domainEvent) => storedEvent = domainEvent)
            .Returns(Task.CompletedTask);

        var registerCommandHandler = new RegisterCommandHandler(userManager, userStore, outbox);

        var result = await registerCommandHandler.Handle(
            new RegisterUserCommand { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User);

        result.AccountId.Should().NotBeEmpty();
        capturedAccount?.Id!.Should().Be(result.AccountId);
        storedEvent.Should().NotBeNull();
        storedEvent.Should().BeOfType<UserRegisteredEvent>();
    }

    [Fact]
    public async Task GivenAUserAccountExistsShouldBeAbleToUpdateDetails()
    {
        var testEmailAddress = "test@test.com";
        var testAccountId = "1234";

        UserAccount? capturedAccount = null;
        var userAccountUnderTest = UserAccount.From(new AccountId(testAccountId), testEmailAddress,
            "John", "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User);

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.UpdateAccount(A<UserAccount>.Ignored))
            .Invokes((UserAccount account) => capturedAccount = account);
        A.CallTo(() => userRepo.WithIdAsync(A<AccountId>.Ignored))
            .ReturnsLazily(() => Task.FromResult<UserAccount?>(userAccountUnderTest));

        var handler = new UpdateUserDetailsHandler(userRepo);

        await handler.Handle(
            new UpdateUserDetailsRequest
                { AccountId = new AccountId(testAccountId), FirstName = "James", LastName = "Eastham" });

        capturedAccount!.FirstName.Should().Be("James");
        capturedAccount!.LastName.Should().Be("Eastham");
        capturedAccount!.DomainEvents.Count.Should().Be(1);

        var firstEvent = capturedAccount!.DomainEvents.First();
        firstEvent.Should().BeOfType<UserDetailsUpdatedEvent>();
    }

    [Fact]
    public async Task GivenAnEmailAlreadyExistsRegistrationShouldFail()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";

        var userManager = A.Fake<UserManager<PostgresUserAccount>>();
        A.CallTo(() => userManager.FindByEmailAsync(A<string>.Ignored))
            .Returns(Task.FromResult<PostgresUserAccount?>(new PostgresUserAccount()));

        var userStore = A.Fake<IUserStore<PostgresUserAccount>>();

        var outbox = A.Fake<IOutbox>();

        var registerCommandHandler = new RegisterCommandHandler(userManager, userStore, outbox);

        await Assert.ThrowsAsync<UserExistsException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User));
    }

}