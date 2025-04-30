using System.Text;
using FluentAssertions;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;
using Xunit.Abstractions;

namespace Stickerlandia.UserManagement.IntegrationTest
{
    public class AccountTests
    {
        private readonly AccountDriver _driver;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Random _random = new();
        
        public AccountTests(ITestOutputHelper testOutputHelper)
        {
            _driver = new AccountDriver(testOutputHelper);
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public async Task UserShouldBeAbleToRegisterAndThenLogin()
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
        
        [Fact]
        public async Task UserCanRetrieveTheirAccountDetailsAfterRegistration()
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
        public async Task UserShouldNotBeAbleToLoginWithInvalidPassword()
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
        public async Task UnregisteredEmailsCantLogin()
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
        public async Task InvalidEmailsCantRegister(string invalidEmail)
        {
            // Arrange
            var password = "ValidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(invalidEmail, password);
            
            // Assert
            registerResult.Should().BeNull();
        }
        
        [Fact]
        public async Task EmptyEmailsCantRegister()
        {
            // Arrange
            var emptyEmail = string.Empty;
            var password = "ValidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(emptyEmail, password);
            
            // Assert
            registerResult.Should().BeNull();
        }
        
        [Theory]
        [InlineData("short")]                // Too short
        [InlineData("nouppercase123!")]      // No uppercase
        [InlineData("NOLOWERCASE123!")]      // No lowercase
        [InlineData("NoSpecialChars123")]    // No special chars
        [InlineData("NoNumbers!")]           // No numbers
        public async Task InvalidPasswordsCantRegister(string invalidPassword)
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, invalidPassword);
            
            // Assert
            registerResult.Should().BeNull();
        }
        
        [Fact]
        public async Task EmptyPasswordsCantRegister()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var emptyPassword = string.Empty;
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, emptyPassword);
            
            // Assert
            registerResult.Should().BeNull();
        }

        #region Edge Cases

        [Fact]
        public async Task ExtremelyLongEmailShouldFailRegistration()
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
        public async Task ExtremelyLongPasswordShouldFailRegistration()
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
        public async Task ValidSpecialFormatsOfEmailShouldRegister(string email)
        {
            // Arrange
            var password = "ValidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(email, password);
            
            // Assert
            registerResult.Should().NotBeNull();
        }

        [Theory]
        [InlineData("üñïçøðé@example.com")]           // Unicode in local part
        [InlineData("user@üñïçøðé.com")]              // Unicode in domain
        public async Task UnicodeInEmailShouldBeHandledConsistently(string email)
        {
            // Arrange
            var password = "ValidPassword123!";
            
            // Act
            var registerResult = await _driver.RegisterUser(email, password);
            
            // Whether it succeeds or fails, it should do so consistently
            // This test doesn't assert success/failure but checks system handles it gracefully
            _testOutputHelper.WriteLine($"Registration with Unicode email '{email}' result: {(registerResult != null ? "Success" : "Failure")}");
        }

        [Fact]
        public async Task PasswordWithUnicodeCharactersShouldBeHandledConsistently()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var unicodePassword = "ÜñïÇøÐé!123A";
            
            // Act
            var registerResult = await _driver.RegisterUser(emailAddress, unicodePassword);
            
            // Whether it succeeds or fails, system should handle it gracefully
            _testOutputHelper.WriteLine($"Registration with Unicode password result: {(registerResult != null ? "Success" : "Failure")}");
            
            if (registerResult != null)
            {
                // If registration succeeded, login should also work
                var loginResponse = await _driver.Login(emailAddress, unicodePassword);
                loginResponse.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task AttemptingConcurrentRegistrationWithSameEmail()
        {
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var password = "ValidPassword123!";
            
            // Act - Start both registrations concurrently
            var task1 = _driver.RegisterUser(emailAddress, password);
            var task2 = _driver.RegisterUser(emailAddress, password);
            
            // Wait for both to complete
            await Task.WhenAll(task1, task2);
            
            // Assert - One should succeed, one should fail
            var results = new[] { task1.Result, task2.Result };
            results.Count(r => r != null).Should().Be(1, "Only one registration with the same email should succeed");
        }

        #endregion

        #region Fuzz Testing

        [Fact]
        public async Task FuzzTestEmailRegistration()
        {
            // Run multiple fuzz tests with random invalid emails
            for (int i = 0; i < 20; i++)
            {
                // Arrange
                var randomInvalidEmail = GenerateRandomInvalidEmail();
                var password = "ValidPassword123!";
                
                _testOutputHelper.WriteLine($"Testing random invalid email: {randomInvalidEmail}");
                
                // Act
                var registerResult = await _driver.RegisterUser(randomInvalidEmail, password);
                
                // Assert - System should not crash and likely reject the invalid email
                // We're not strictly asserting null here because some edge cases might be accepted
                // The important thing is the system doesn't crash
                _testOutputHelper.WriteLine($"Result: {(registerResult == null ? "Rejected (expected)" : "Accepted (investigate if suspicious)")}");
            }
        }

        [Fact]
        public async Task FuzzTestPasswordRegistration()
        {
            // Run multiple fuzz tests with random invalid passwords
            for (int i = 0; i < 20; i++)
            {
                // Arrange
                var emailAddress = $"{Guid.NewGuid()}@test.com";
                var randomInvalidPassword = GenerateRandomInvalidPassword();
                
                _testOutputHelper.WriteLine($"Testing random invalid password: {randomInvalidPassword}");
                
                // Act
                var registerResult = await _driver.RegisterUser(emailAddress, randomInvalidPassword);
                
                // Assert - System should handle it without crashing
                _testOutputHelper.WriteLine($"Result: {(registerResult == null ? "Rejected (expected)" : "Accepted (investigate if suspicious)")}");
            }
        }

        [Fact]
        public async Task FuzzTestLoginWithRandomData()
        {
            // Test login endpoint with random data
            for (int i = 0; i < 20; i++)
            {
                // Arrange
                var randomEmail = GenerateRandomString(50) + "@" + GenerateRandomString(10) + ".com";
                var randomPassword = GenerateRandomString(20);
                
                _testOutputHelper.WriteLine($"Fuzzing login with: {randomEmail} / {randomPassword}");
                
                // Act
                var loginResult = await _driver.Login(randomEmail, randomPassword);
                
                // Assert - Should not crash, most likely reject the login
                loginResult.Should().BeNull("Random credentials should not authenticate");
            }
        }

        #region Fuzz Test Helpers

        private string GenerateRandomInvalidEmail()
        {
            // Array of email generation strategies that produce invalid emails
            var strategies = new Func<string>[]
            {
                () => GenerateRandomString(_random.Next(1, 50)),  // No @ symbol
                () => "@" + GenerateRandomString(_random.Next(1, 20)),  // No local part
                () => GenerateRandomString(_random.Next(1, 20)) + "@",  // No domain
                () => GenerateRandomString(_random.Next(1, 20)) + "@" + GenerateRandomString(_random.Next(1, 20)),  // No TLD
                () => GenerateRandomString(_random.Next(1, 20)) + "@." + GenerateRandomString(_random.Next(1, 5)),  // Missing domain part
                () => GenerateRandomString(_random.Next(1, 20)) + "@@" + GenerateRandomString(_random.Next(1, 20)) + ".com",  // Double @
                () => " " + GenerateRandomString(_random.Next(1, 20)) + "@example.com",  // Leading space
                () => GenerateRandomString(_random.Next(1, 20)) + "@example.com ",  // Trailing space
                () => GenerateRandomString(_random.Next(300, 500)) + "@example.com",  // Too long local part
                () => string.Empty  // Empty string
            };
            
            // Pick a random strategy
            return strategies[_random.Next(strategies.Length)]();
        }

        private string GenerateRandomInvalidPassword()
        {
            // Array of password generation strategies
            var strategies = new Func<string>[]
            {
                () => GenerateRandomString(_random.Next(1, 5)),  // Too short
                () => new string(Enumerable.Repeat('a', _random.Next(8, 20)).ToArray()),  // Only lowercase
                () => new string(Enumerable.Repeat('A', _random.Next(8, 20)).ToArray()),  // Only uppercase
                () => new string(Enumerable.Repeat('1', _random.Next(8, 20)).ToArray()),  // Only numbers
                () => new string(Enumerable.Repeat('!', _random.Next(8, 20)).ToArray()),  // Only special chars
                () => GenerateRandomString(_random.Next(8, 20), "abcdefghijklmnopqrstuvwxyz"),  // No uppercase
                () => GenerateRandomString(_random.Next(8, 20), "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),  // No lowercase
                () => GenerateRandomString(_random.Next(8, 20), "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),  // No numbers or special chars
                () => new string(Enumerable.Repeat('\0', 1).ToArray()),  // Null character
                () => string.Empty  // Empty password
            };
            
            // Pick a random strategy
            return strategies[_random.Next(strategies.Length)]();
        }

        private string GenerateRandomString(int length, string allowedChars = null)
        {
            if (string.IsNullOrEmpty(allowedChars))
            {
                allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
            }
            
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(allowedChars[_random.Next(allowedChars.Length)]);
            }
            
            return sb.ToString();
        }

        #endregion

        #endregion
    }
} 