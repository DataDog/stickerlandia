using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Auth;
using Stickerlandia.UserManagement.Core.Login;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Stickerlandia.UserManagement.Core.UpdateUserDetails;

namespace Stickerlandia.UserManagement.UnitTest;

public class AccountTests
{
    [Fact]
    public async Task GivenAUserHasValidDetails_ShouldRegisterSuccessfully()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testAccountId = "1234";

        UserAccount? capturedAccount = null;

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.Add(A<UserAccount>.Ignored))
            .Invokes((UserAccount account) => capturedAccount = account)
            .Returns(Task.FromResult(UserAccount.From(new AccountId(testAccountId), testEmailAddress, testPassword,
                "John", "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User)));

        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var result = await registerCommandHandler.Handle(
            new RegisterUserCommand { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User);

        result.AccountId.Should().NotBeEmpty();
        capturedAccount?.Id.Value.Should().Be(result.AccountId);
        capturedAccount?.DomainEvents.Count.Should().Be(1);

        var firstEvent = capturedAccount!.DomainEvents.First();
        firstEvent.Should().BeOfType<UserRegisteredEvent>();
    }
    
    [Fact]
    public async Task GivenAUserAccountExists_ShouldBeAbleToUpdateDetails()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testAccountId = "1234";

        UserAccount? capturedAccount = null;
        UserAccount? userAccountUnderTest = UserAccount.From(new AccountId(testAccountId), testEmailAddress,
            testPassword,
            "John", "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User);

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.UpdateAccount(A<UserAccount>.Ignored))
            .Invokes((UserAccount account) => capturedAccount = account);
        A.CallTo(() => userRepo.WithIdAsync(A<AccountId>.Ignored))
            .Returns(Task.FromResult(userAccountUnderTest));

        var handler = new UpdateUserDetailsHandler(userRepo);

        await handler.Handle(
            new UpdateUserDetailsRequest() { AccountId = new AccountId(testAccountId), FirstName = "James", LastName = "Eastham"});

        capturedAccount.FirstName.Should().Be("James");
        capturedAccount.LastName.Should().Be("Eastham");
        capturedAccount?.DomainEvents.Count.Should().Be(1);

        var firstEvent = capturedAccount!.DomainEvents.First();
        firstEvent.Should().BeOfType<UserDetailsUpdatedEvent>();
    }

    [Fact]
    public async Task GivenAnEmailAlreadyExists_RegistrationShouldFail()
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
    public async Task GivenAUserRegistersWithAnInvalidEmail_RegistrationShouldFail()
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
    public async Task GivenAUserLogsInWithAnInvalidPassword_AInvalidUserExceptionShouldBeThrown()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "this does not meet password requirements";

        var userRepo = A.Fake<IUsers>();

        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var exception = await Assert.ThrowsAsync<InvalidUserException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User));

        exception.Reason.Should().Be("Invalid password");
    }

    [Fact]
    public async Task GivenAUserLogsInSuccessfully_AValidJwtShouldBeReturned()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testPasswordHash = "gY+ZhJfJftbzJvLmWE6grZgJv3nWkepeIigpSSnDwqY1xiQ1MkSAU16LE+3EljRw";
        var testAccountId = "1234";

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.WithEmailAsync(A<string>.Ignored)).Returns(
            Task.FromResult(UserAccount.From(new AccountId(testAccountId), testEmailAddress, testPasswordHash, "John",
                "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User)));

        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.GenerateAuthToken(A<UserAccount>.Ignored)).Returns("atoken");

        var loginCommandHandler = new LoginCommandHandler(userRepo, authService);

        var loginResult = await loginCommandHandler.Handle(new LoginCommand
        {
            EmailAddress = testEmailAddress,
            Password = testPassword
        });

        loginResult.AuthToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GivenAUserLoginFails_ALoginFailedExceptionIsCalled()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "not the correct password4";
        var testPasswordHash = "gY+ZhJfJftbzJvLmWE6grZgJv3nWkepeIigpSSnDwqY1xiQ1MkSAU16LE+3EljRw";
        var testAccountId = "1234";

        var userRepo = A.Fake<IUsers>();

        A.CallTo(() => userRepo.WithEmailAsync(A<string>.Ignored)).Returns(
            Task.FromResult(UserAccount.From(new AccountId(testAccountId), testEmailAddress, testPasswordHash, "John",
                "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User)));

        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.GenerateAuthToken(A<UserAccount>.Ignored)).Returns("atoken");

        var loginCommandHandler = new LoginCommandHandler(userRepo, authService);

        await Assert.ThrowsAsync<LoginFailedException>(async () => await loginCommandHandler.Handle(new LoginCommand
        {
            EmailAddress = testEmailAddress,
            Password = testPassword
        }));
    }

    [Fact]
    public async Task GivenAUserIsLoggedIn_TheyCanRetrieveTheirUserDetails()
    {
        var testEmailAddress = "test@test.com";
        var testPasswordHash = "gY+ZhJfJftbzJvLmWE6grZgJv3nWkepeIigpSSnDwqY1xiQ1MkSAU16LE+3EljRw";
        var testAccountId = "1234";

        var userRepo = A.Fake<IUsers>();
        A.CallTo(() => userRepo.WithIdAsync(A<AccountId>.Ignored)).Returns(
            Task.FromResult(UserAccount.From(new AccountId(testAccountId), testEmailAddress, testPasswordHash, "John",
                "Doe", 1, DateTime.UtcNow, AccountTier.Std, AccountType.User)));

        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.ValidateAuthToken(A<string>.Ignored)).Returns(new AuthorizedUserDetails
        {
            AccountId = new AccountId(testAccountId),
            Role = "user",
            UserTier = "Std"
        });

        var accountDetailsHandler = new AccountDetailsHandler(userRepo, authService);

        var accountDetails = await accountDetailsHandler.GetAccountByAuthToken("atestauthtoken");

        accountDetails.Should().NotBeNull();
    }
}