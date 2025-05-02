using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;
using Stickerlandia.UserManagement.IntegrationTest.Hooks;
using Xunit.Abstractions;

namespace Stickerlandia.UserManagement.IntegrationTest
{
    public class AccountTests// : IClassFixture<TestSetupFixture>
    {
        private readonly AccountDriver _driver;
        private readonly ITestOutputHelper _testOutputHelper;
        
        public AccountTests(ITestOutputHelper testOutputHelper)//, TestSetupFixture testSetupFixture)
        {
            //_driver = new AccountDriver(testOutputHelper, testSetupFixture);
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public async Task UserShouldBeAbleToRegisterAndThenLogin()
        {
            // Run all local resources with Asipre for testing
            var builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Stickerlandia_UserManagement_Aspire>(
                    args: ["DcpPublisher:RandomizePorts=false"],
                    configureBuilder: (appOptions, host) =>
                    {
                        appOptions.DisableDashboard = false;
                        appOptions.EnableResourceLogging = true;
                        appOptions.AllowUnsecuredTransport = true;
                    });
            builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });
            
            builder.Configuration["RUN_AS"] = Environment.GetEnvironmentVariable("RUN_AS") ?? "ASPNET";

            await using var app = await builder.BuildAsync();

            await app.StartAsync();
            
            var httpClient = app.CreateHttpClient("api");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await app.ResourceNotifications.WaitForResourceHealthyAsync(
                "api",
                cts.Token);
            
            // When Azure Functions is used, the API is not available immediately even when the container is healthy.
            await Task.Delay(TimeSpan.FromSeconds(10));

            var messagingConnectionString = await app.GetConnectionStringAsync("messaging");
            
            if (string.IsNullOrEmpty(messagingConnectionString))
            {
                throw new Exception("Messaging connection string is not set.");
            }

            var messaging = new AzureServiceBusMessaging(messagingConnectionString);
            
            var apiDriver = new AccountDriver(_testOutputHelper, httpClient, messaging);
            
            // Arrange
            var emailAddress = $"{Guid.NewGuid()}@test.com";
            var password = $"{Guid.NewGuid()}!A23";
            
            // Act
            var registerResult = await apiDriver.RegisterUser(emailAddress, password);
            var loginResponse = await apiDriver.Login(emailAddress, password);
            
            // Assert
            registerResult.Should().NotBeNull();
            loginResponse.Should().NotBeNull();
            loginResponse!.AuthToken.Should().NotBeEmpty();
        }
        
        // [Fact]
        // public async Task UserShouldBeAbleToRegisterAndThenUpdateDetails()
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     var password = $"{Guid.NewGuid()}!A23";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, password);
        //     var loginResponse = await _driver.Login(emailAddress, password);
        //     
        //     await _driver.UpdateUserDetails(loginResponse!.AuthToken, "James", "Eastham");
        //     
        //     var userDetails = await _driver.GetUserAccount(loginResponse!.AuthToken);
        //     
        //     // Assert
        //     userDetails.FirstName.Should().Be("James");
        //     userDetails.LastName.Should().Be("Eastham");
        // }
        //
        // [Fact]
        // public async Task WhenStickerIsClaimedUsersStickerCountShouldIncrement()
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     var password = $"{Guid.NewGuid()}!A23";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, password);
        //
        //     if (registerResult is null)
        //     {
        //         throw new Exception("Registration failed");
        //     }
        //     
        //     var loginResponse = await _driver.Login(emailAddress, password);
        //     
        //     if (loginResponse is null)
        //     {
        //         throw new Exception("Login response is null");
        //     }
        //     await _driver.InjectStickerClaimedMessage(registerResult.AccountId, Guid.NewGuid().ToString());
        //     
        //     await Task.Delay(TimeSpan.FromSeconds(5));
        //
        //     var user = await _driver.GetUserAccount(loginResponse.AuthToken);
        //     
        //     user!.ClaimedStickerCount.Should().Be(1);
        // }
        //
        // [Fact]
        // public async Task UserCanRetrieveTheirAccountDetailsAfterRegistration()
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     var password = $"{Guid.NewGuid()}!A23";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, password);
        //     var loginResponse = await _driver.Login(emailAddress, password);
        //     var userAccount = await _driver.GetUserAccount(loginResponse!.AuthToken);
        //     
        //     // Assert
        //     registerResult.Should().NotBeNull();
        //     loginResponse.Should().NotBeNull();
        //     userAccount.Should().NotBeNull();
        //     userAccount!.EmailAddress.Should().Be(emailAddress);
        // }
        //
        // [Fact]
        // public async Task UserShouldNotBeAbleToLoginWithInvalidPassword()
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     var validPassword = $"{Guid.NewGuid()}!A23";
        //     var invalidPassword = "InvalidPassword123!";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, validPassword);
        //     var loginResponse = await _driver.Login(emailAddress, invalidPassword);
        //     
        //     // Assert
        //     registerResult.Should().NotBeNull();
        //     loginResponse.Should().BeNull();
        // }
        //
        // [Fact]
        // public async Task UnregisteredEmailsCantLogin()
        // {
        //     // Arrange
        //     var unregisteredEmail = $"{Guid.NewGuid()}@test.com";
        //     var password = "ValidPassword123!";
        //     
        //     // Act
        //     var loginResponse = await _driver.Login(unregisteredEmail, password);
        //     
        //     // Assert
        //     loginResponse.Should().BeNull();
        // }
        //
        // [Theory]
        // [InlineData("invalidemailformat")]
        // [InlineData("@missingusername.com")]
        // [InlineData("missing@tld")]
        // public async Task InvalidEmailsCantRegister(string invalidEmail)
        // {
        //     // Arrange
        //     var password = "ValidPassword123!";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(invalidEmail, password);
        //     
        //     // Assert
        //     registerResult.Should().BeNull();
        // }
        //
        // [Fact]
        // public async Task EmptyEmailsCantRegister()
        // {
        //     // Arrange
        //     var emptyEmail = string.Empty;
        //     var password = "ValidPassword123!";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emptyEmail, password);
        //     
        //     // Assert
        //     registerResult.Should().BeNull();
        // }
        //
        // [Theory]
        // [InlineData("short")]                // Too short
        // [InlineData("nouppercase123!")]      // No uppercase
        // [InlineData("NOLOWERCASE123!")]      // No lowercase
        // [InlineData("NoSpecialChars123")]    // No special chars
        // [InlineData("NoNumbers!")]           // No numbers
        // public async Task InvalidPasswordsCantRegister(string invalidPassword)
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, invalidPassword);
        //     
        //     // Assert
        //     registerResult.Should().BeNull();
        // }
        //
        // [Fact]
        // public async Task EmptyPasswordsCantRegister()
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     var emptyPassword = string.Empty;
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, emptyPassword);
        //     
        //     // Assert
        //     registerResult.Should().BeNull();
        // }
        //
        // [Fact]
        // public async Task ExtremelyLongEmailShouldFailRegistration()
        // {
        //     // Arrange
        //     var longLocalPart = new string('a', 300);
        //     var longEmail = $"{longLocalPart}@example.com";
        //     var password = "ValidPassword123!";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(longEmail, password);
        //     
        //     // Assert
        //     registerResult.Should().BeNull();
        // }
        //
        // [Fact]
        // public async Task ExtremelyLongPasswordShouldFailRegistration()
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     var longPassword = new string('A', 100) + new string('a', 100) + new string('1', 100) + new string('!', 100);
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, longPassword);
        //     
        //     // Assert
        //     registerResult.Should().BeNull();
        // }
        //
        // [Theory]
        // [InlineData("test+tag@example.com")]          // Gmail-style tags
        // [InlineData("test.email@example.com")]        // Dots in local part
        // [InlineData("email-with-hyphen@example.com")] // Hyphens
        // [InlineData("email_with_underscore@example.com")] // Underscores
        // public async Task ValidSpecialFormatsOfEmailShouldRegister(string email)
        // {
        //     // Arrange
        //     var password = "ValidPassword123!";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(email, password);
        //     
        //     // Assert
        //     registerResult.Should().NotBeNull();
        // }
        //
        // [Theory]
        // [InlineData("üñïçøðé@example.com")]           // Unicode in local part
        // [InlineData("user@üñïçøðé.com")]              // Unicode in domain
        // public async Task UnicodeInEmailShouldBeHandledConsistently(string email)
        // {
        //     // Arrange
        //     var password = "ValidPassword123!";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(email, password);
        //     
        //     // Whether it succeeds or fails, it should do so consistently
        //     // This test doesn't assert success/failure but checks system handles it gracefully
        //     _testOutputHelper.WriteLine($"Registration with Unicode email '{email}' result: {(registerResult != null ? "Success" : "Failure")}");
        // }
        //
        // [Fact]
        // public async Task PasswordWithUnicodeCharactersShouldBeHandledConsistently()
        // {
        //     // Arrange
        //     var emailAddress = $"{Guid.NewGuid()}@test.com";
        //     var unicodePassword = "ÜñïÇøÐé!123A";
        //     
        //     // Act
        //     var registerResult = await _driver.RegisterUser(emailAddress, unicodePassword);
        //     
        //     // Whether it succeeds or fails, system should handle it gracefully
        //     _testOutputHelper.WriteLine($"Registration with Unicode password result: {(registerResult != null ? "Success" : "Failure")}");
        //     
        //     if (registerResult != null)
        //     {
        //         // If registration succeeded, login should also work
        //         var loginResponse = await _driver.Login(emailAddress, unicodePassword);
        //         loginResponse.Should().NotBeNull();
        //     }
        // }
    }
} 