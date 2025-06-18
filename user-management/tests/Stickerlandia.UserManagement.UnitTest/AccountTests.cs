using System.Collections.Immutable;
using System.Security.Claims;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Login;
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
        var testAccountId = "1234";

        UserAccount? capturedAccount = null;

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.Add(A<UserAccount>.Ignored))
            .Invokes((UserAccount account) => capturedAccount = account)
            .Returns(Task.FromResult(UserAccount.From(new AccountId(testAccountId), testEmailAddress,
                "John", "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User)));

        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var result = await registerCommandHandler.Handle(
            new RegisterUserCommand { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User);

        result.AccountId.Should().NotBeEmpty();
        capturedAccount?.Id!.Value.Should().Be(result.AccountId);
        capturedAccount?.DomainEvents.Count.Should().Be(1);

        var firstEvent = capturedAccount!.DomainEvents.First();
        firstEvent.Should().BeOfType<UserRegisteredEvent>();
    }
    
    [Fact]
    public async Task GivenAUserAccountExistsShouldBeAbleToUpdateDetails()
    {
        var testEmailAddress = "test@test.com";
        var testAccountId = "1234";

        UserAccount? capturedAccount = null;
        UserAccount? userAccountUnderTest = UserAccount.From(new AccountId(testAccountId), testEmailAddress,
            "John", "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User);

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.UpdateAccount(A<UserAccount>.Ignored))
            .Invokes((UserAccount account) => capturedAccount = account);
        A.CallTo(() => userRepo.WithIdAsync(A<AccountId>.Ignored))
            .ReturnsLazily(() => Task.FromResult<UserAccount?>(userAccountUnderTest));

        var handler = new UpdateUserDetailsHandler(userRepo);

        await handler.Handle(
            new UpdateUserDetailsRequest() { AccountId = new AccountId(testAccountId), FirstName = "James", LastName = "Eastham"});

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

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.DoesEmailExistAsync(A<string>.Ignored)).Returns(Task.FromResult(true));

        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        await Assert.ThrowsAsync<UserExistsException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User));
    }

    [Fact]
    public async Task GivenAUserRegistersWithAnInvalidEmailRegistrationShouldFail()
    {
        var testEmailAddress = "@test.com";
        var testPassword = "Password!234";

        var userRepo = A.Fake<IUsers>();

        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var exception = await Assert.ThrowsAsync<InvalidUserException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User));

        exception.Reason.Should().Be("Invalid email address");
    }

    [Fact]
    public async Task GivenAUserLogsInSuccessfullyAValidJwtShouldBeReturned()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testAccountId = "1234";

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.WithEmailAsync(A<string>.Ignored))!.Returns(
            Task.FromResult(UserAccount.From(new AccountId(testAccountId), testEmailAddress, "John",
                "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User)));

        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.VerifyPassword(A<string>.Ignored, A<string>.Ignored, A<ImmutableArray<string>>.Ignored))
            .Returns(new ClaimsIdentity());

        var loginCommandHandler = new LoginCommandHandler(userRepo, authService);

        var loginResult = await loginCommandHandler.Handle(new LoginCommand
        {
            EmailAddress = testEmailAddress,
            Password = testPassword
        });

        loginResult.Identity.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenAUserLoginFailsALoginFailedExceptionIsCalled()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "not the correct password4";
        var testAccountId = "1234";

        var userRepo = A.Fake<IUsers>();
        
        A.CallTo(() => userRepo.WithEmailAsync(A<string>.Ignored))
            .ReturnsLazily(() => Task.FromResult<UserAccount?>(UserAccount.From(new AccountId(testAccountId), testEmailAddress, "John",
                "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User)));

        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.VerifyPassword(A<string>.Ignored, A<string>.Ignored, A<ImmutableArray<string>>.Ignored))
            .Throws<LoginFailedException>();

        var loginCommandHandler = new LoginCommandHandler(userRepo, authService);

        try
        {
            await loginCommandHandler.Handle(new LoginCommand
            {
                EmailAddress = testEmailAddress,
                Password = testPassword
            });
            
            Assert.Fail("Expected LoginFailedException to be thrown, but it was not.");
        }
        catch (LoginFailedException)
        {
        }
        catch (Exception ex)
        {
            Assert.Fail("An exception was thrown that was not a LoginFailedException: " + ex.Message);
        }
    }
}