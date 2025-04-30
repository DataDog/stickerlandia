using System.Text.Json;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Auth;
using Stickerlandia.UserManagement.Core.Login;
using Stickerlandia.UserManagement.Core.Register;

namespace Stickerlandia.UserManagement.UnitTest;

public class AccountTests
{
    [Fact]
    public async Task CanRegisterNewUser_ShouldAllowRegisterWithValidDetails()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testAccountId = "1234";
        UserAccount capturedAccount = null;
        
        var userRepo = A.Fake<IUserAccountRepository>();
        A.CallTo(() => userRepo.CreateAccount(A<UserAccount>.Ignored))
            .Invokes((UserAccount account) => capturedAccount = account)
            .Returns(Task.FromResult(UserAccount.From(testAccountId, testEmailAddress, testPassword, "John", "Doe", DateTime.UtcNow, AccountTier.Std, AccountType.User)));
        
        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var result = await registerCommandHandler.Handle(
            new RegisterUserCommand() { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User);

        result.AccountId.Should().NotBeEmpty();
        capturedAccount?.Id.Should().Be(result.AccountId);
        capturedAccount.DomainEvents.Count.Should().Be(1);
        
        var firstEvent = capturedAccount.DomainEvents.First();
        firstEvent.Should().BeOfType<UserRegisteredEvent>();
    }
    
    [Fact]
    public async Task CantRegisterWithInvalidStaffEmail()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testAccountId = "1234";
        
        var userRepo = A.Fake<IUserAccountRepository>();
        
        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var exception = await Assert.ThrowsAsync<InvalidUserException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand() { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.Staff));

        exception.Reason.Should().Be("Not a valid staff email");
    }
    
    [Fact]
    public async Task CantRegisterWithExistingEmail()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testAccountId = "1234";
        
        var userRepo = A.Fake<IUserAccountRepository>();
        A.CallTo(() => userRepo.DoesEmailExistAsync(A<string>.Ignored)).Returns(Task.FromResult(true));
        
        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        await Assert.ThrowsAsync<UserExistsException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand() { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User));
    }
    
    [Fact]
    public async Task CantRegisterWithInvalidEmail()
    {
        var testEmailAddress = "@test.com";
        var testPassword = "Password!234";
        var testAccountId = "1234";
        
        var userRepo = A.Fake<IUserAccountRepository>();
        
        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var exception = await Assert.ThrowsAsync<InvalidUserException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand() { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User));

        exception.Reason.Should().Be("Invalid email address");
    }
    
    [Fact]
    public async Task CantRegisterWithInvalidPassword()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "this does not meet password requirements";
        var testAccountId = "1234";
        
        var userRepo = A.Fake<IUserAccountRepository>();
        
        var registerCommandHandler = new RegisterCommandHandler(userRepo);

        var exception = await Assert.ThrowsAsync<InvalidUserException>(async () => await registerCommandHandler.Handle(
            new RegisterUserCommand() { EmailAddress = testEmailAddress, Password = testPassword }, AccountType.User));

        exception.Reason.Should().Be("Invalid password");
    }
    
    [Fact]
    public async Task OnSuccessfulLogin_ShouldReturnValidJwt()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testPasswordHash = "gY+ZhJfJftbzJvLmWE6grZgJv3nWkepeIigpSSnDwqY1xiQ1MkSAU16LE+3EljRw";
        var testAccountId = "1234";
        
        var userRepo = A.Fake<IUserAccountRepository>();
        A.CallTo(() => userRepo.GetAccountByEmailAsync(A<string>.Ignored)).Returns(
            Task.FromResult(UserAccount.From(testAccountId, testEmailAddress, testPasswordHash, "John", "Doe", DateTime.UtcNow, AccountTier.Std, AccountType.User)));
        
        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.GenerateAuthToken(A<UserAccount>.Ignored)).Returns("atoken");

        var loginCommandHandler = new LoginCommandHandler(userRepo, authService);

        var loginResult = await loginCommandHandler.Handle(new LoginCommand()
        {
            EmailAddress = testEmailAddress,
            Password = testPassword
        });

        loginResult.AuthToken.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task OnFailedLogin_ShouldNotReturnJwt()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "not the correct password4";
        var testPasswordHash = "gY+ZhJfJftbzJvLmWE6grZgJv3nWkepeIigpSSnDwqY1xiQ1MkSAU16LE+3EljRw";
        var testAccountId = "1234";
        
        var userRepo = A.Fake<IUserAccountRepository>();

        A.CallTo(() => userRepo.GetAccountByEmailAsync(A<string>.Ignored)).Returns(
            Task.FromResult(UserAccount.From(testAccountId, testEmailAddress, testPasswordHash, "John", "Doe", DateTime.UtcNow, AccountTier.Std, AccountType.User)));
        
        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.GenerateAuthToken(A<UserAccount>.Ignored)).Returns("atoken");

        var loginCommandHandler = new LoginCommandHandler(userRepo, authService);

        await Assert.ThrowsAsync<LoginFailedException>(async () => await loginCommandHandler.Handle(new LoginCommand()
        {
            EmailAddress = testEmailAddress,
            Password = testPassword
        }));
    }
    
    [Fact]
    public async Task CanRetrieveUserDetails()
    {
        var testEmailAddress = "test@test.com";
        var testPassword = "Password!234";
        var testPasswordHash = "gY+ZhJfJftbzJvLmWE6grZgJv3nWkepeIigpSSnDwqY1xiQ1MkSAU16LE+3EljRw";
        var testAccountId = "1234";
        
        var userRepo = A.Fake<IUserAccountRepository>();
        A.CallTo(() => userRepo.GetAccountByIdAsync(A<string>.Ignored)).Returns(
            Task.FromResult(UserAccount.From(testAccountId, testEmailAddress, testPasswordHash, "John", "Doe", DateTime.UtcNow, AccountTier.Std, AccountType.User)));
        
        var authService = A.Fake<IAuthService>();
        A.CallTo(() => authService.ValidateAuthToken(A<string>.Ignored)).Returns(new AuthorizedUserDetails
        {
            AccountId = testAccountId,
            Role = "user",
            UserTier = "Std" 
        });

        var accountDetailsHandler = new AccountDetailsHandler(userRepo, authService);

        var accountDetails = await accountDetailsHandler.GetAccountByAuthToken("atestauthtoken");

        accountDetails.Should().NotBeNull();
    }
}