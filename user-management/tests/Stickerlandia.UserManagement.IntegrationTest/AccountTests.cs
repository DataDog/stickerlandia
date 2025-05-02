using FluentAssertions;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;
using Stickerlandia.UserManagement.IntegrationTest.Hooks;
using Xunit.Abstractions;

namespace Stickerlandia.UserManagement.IntegrationTest
{
    public class AccountTests : IClassFixture<TestSetupFixture>
    {
        private readonly AccountDriver _driver;
        private readonly ITestOutputHelper _testOutputHelper;
        
        public AccountTests(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
        {
            _driver = new AccountDriver(testOutputHelper, testSetupFixture.HttpClient, testSetupFixture.Messaging);
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public async Task WhenStickerIsClaimed_ThenAUsersStickerCountShouldIncrement()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var password = $"{Guid.NewGuid()}!A23";
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, password);
        
            if (registerResult is null)
            {
                throw new Exception("Registration failed");
            }
            
            var loginResponse = await _driver.Login(emailAddress, password);
            
            if (loginResponse is null)
            {
                throw new Exception("Login response is null");
            }
            await _driver.InjectStickerClaimedMessage(registerResult.AccountId, Guid.NewGuid().ToString());
            
            await Task.Delay(TimeSpan.FromSeconds(5));
        
            var user = await _driver.GetUserAccount(loginResponse.AuthToken);
            
            user!.ClaimedStickerCount.Should().Be(1);
        }
        
        [Fact]
        public async Task WhenAUserRegisters_ThenTheyShouldBeAbleToLogin()
        {
            try
            {
                // Arrange
                var emailAddress = $"{Guid.NewGuid()}@test.com";
                var password = $"{Guid.NewGuid()}!A23";
        
                // Act
                var registerResult = await _driver.RegisterUser(emailAddress, password);
                var loginResponse = await _driver.Login(emailAddress, password);
        
                // Assert
                registerResult.Should().NotBeNull();
                loginResponse.Should().NotBeNull();
                loginResponse!.AuthToken.Should().NotBeEmpty();
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine(ex.Message);
                _testOutputHelper.WriteLine(ex.StackTrace);
                
                // Wait for logs to flish
                await Task.Delay(TimeSpan.FromSeconds(10));
                throw;
            }
            
        }
        
        [Fact]
        public async Task WhenAUserRegisters_TheyShouldBeAbleToUpdateTheirDetails()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var password = $"{Guid.NewGuid()}!A23";
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, password);
            var loginResponse = await _driver.Login(emailAddress, password);
            
            await _driver.UpdateUserDetails(loginResponse!.AuthToken, "James", "Eastham");
            
            var userDetails = await _driver.GetUserAccount(loginResponse!.AuthToken);
            
            // Assert
            userDetails.FirstName.Should().Be("James");
            userDetails.LastName.Should().Be("Eastham");
        }
        
        [Fact]
        public async Task WhenAUserRegisters_ThenCanRetrieveTheirAccountDetails()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var password = $"{Guid.NewGuid()}!A23";
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, password);
            var loginResponse = await _driver.Login(emailAddress, password);
            var userAccount = await _driver.GetUserAccount(loginResponse!.AuthToken);
            
            // Assert
            registerResult.Should().NotBeNull();
            loginResponse.Should().NotBeNull();
            userAccount.Should().NotBeNull();
            userAccount!.EmailAddress.Should().Be(emailAddress);
        }
        
        [Fact]
        public async Task WhenAUserLogsInWithAnInvalidPassword_ThenLoginFails()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var validPassword = $"{Guid.NewGuid()}!A23";
            var invalidPassword = "InvalidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, validPassword);
            var loginResponse = await _driver.Login(emailAddress, invalidPassword);
            
            // Assert
            registerResult.Should().NotBeNull();
            loginResponse.Should().BeNull();
        }
        
        [Fact]
        public async Task WhenAUserLogsInWithAnUnregisteredEmail_LoginShouldFail()
        {
            // Arrange
            var unregisteredEmail = $"{Guid.NewGuid()}@test.com";
            var password = "ValidPassword123!";
            
            // Act
            var loginResponse = await _driver.Login(unregisteredEmail, password);
            
            // Assert
            loginResponse.Should().BeNull();
        }
        
        [Theory]
        [InlineData("invalidemailformat")]
        [InlineData("@missingusername.com")]
        [InlineData("missing@tld")]
        [InlineData("")]
        public async Task WhenAUserUsesAnInvalidEmail_RegistrationShouldFail(string invalidEmail)
        {
            // Arrange
            var password = "ValidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(invalidEmail, password);
            
            // Assert
            registerResult.Should().BeNull();
        }
        
        [Theory]
        [InlineData("short")]                // Too short
        [InlineData("nouppercase123!")]      // No uppercase
        [InlineData("NOLOWERCASE123!")]      // No lowercase
        [InlineData("NoSpecialChars123")]    // No special chars
        [InlineData("NoNumbers!")]           // No numbers
        [InlineData("")]           // Empty
        public async Task WhenAUserRegistersWithAnInvalidPassword_RegistrationShouldFail(string invalidPassword)
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, invalidPassword);
            
            // Assert
            registerResult.Should().BeNull();
        }
        
        [Fact]
        public async Task WhenAUserUsesAnExtremelyLongEmailAddress_RegistrationShouldFail()
        {
            // Arrange
            var longLocalPart = new string('a', 300);
            var longEmail = $"{longLocalPart}@example.com";
            var password = "ValidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(longEmail, password);
            
            // Assert
            registerResult.Should().BeNull();
        }
        
        [Fact]
        public async Task WhenAUserUsesAnExtremelyLongPassword_RegistrationShouldFail()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var longPassword = new string('A', 100) + new string('a', 100) + new string('1', 100) + new string('!', 100);
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, longPassword);
            
            // Assert
            registerResult.Should().BeNull();
        }
        
        [Theory]
        [InlineData("test+tag@example.com")]          // Gmail-style tags
        [InlineData("test.email@example.com")]        // Dots in local part
        [InlineData("email-with-hyphen@example.com")] // Hyphens
        [InlineData("email_with_underscore@example.com")] // Underscores
        public async Task WhenAUserUsesASpecialEmailFormat_RegistrationShouldBeSuccessful(string email)
        {
            // Arrange
            var password = "ValidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(email, password);
            
            // Assert
            registerResult.Should().NotBeNull();
        }
    }
} 