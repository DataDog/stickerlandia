/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Net;
using FluentAssertions;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;
using Stickerlandia.UserManagement.IntegrationTest.Hooks;
using Xunit.Abstractions;

namespace Stickerlandia.UserManagement.IntegrationTest;

[Collection("Integration Tests")]
public sealed class AccountTests(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
    : IDisposable
{
    private readonly AccountDriver _driver = new(testOutputHelper, testSetupFixture.HttpClient,
        testSetupFixture.Messaging, new CookieContainer());

    [Fact]
    public async Task WhenStickerIsClaimedThenAUsersStickerCountShouldIncrement()
    {
        // Arrange
        var emailAddress = $"{Guid.NewGuid()}@test.com";
        var password = $"{Guid.NewGuid()}!A23";

        // Act
        var registerResult = await _driver.RegisterUser(emailAddress, password);

        if (registerResult is null) throw new ArgumentException("Registration failed");

        var loginResponse = await _driver.Login(emailAddress, password);

        if (loginResponse is null) throw new ArgumentException("Login response is null");
        
        var userAccount = await _driver.GetUserAccount(loginResponse.AuthToken);
        
        await _driver.InjectStickerClaimedMessage(userAccount.AccountId, Guid.NewGuid().ToString());

        await Task.Delay(TimeSpan.FromSeconds(5));

        var retryCount = 1;
        var maxRetries = 5;

        while (retryCount <= maxRetries)
        {
            testOutputHelper.WriteLine($"Retry {retryCount} of {maxRetries} to check sticker count...");
            
            var user = await _driver.GetUserAccount(loginResponse.AuthToken);

            // Expect the claimed sticker count to be 1, break after completed.
            if (user!.ClaimedStickerCount == 1)
            {
                break;
            }
            
            retryCount++;

            if (retryCount == maxRetries) Assert.Fail("Failed to increment sticker count after maximum retries.");

            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    [Fact]
    public async Task WhenAUserRegistersTheyShouldBeAbleToUpdateTheirDetails()
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
        userDetails!.FirstName.Should().Be("James");
        userDetails!.LastName.Should().Be("Eastham");
    }

    [Fact]
    public async Task WhenAUserLogsInWithAnInvalidPasswordThenLoginFails()
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
    public async Task WhenAUserLogsInWithAnUnregisteredEmailLoginShouldFail()
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
    [InlineData("")]
    public async Task WhenAUserUsesAnInvalidEmailRegistrationShouldFail(string invalidEmail)
    {
        // Arrange
        var password = "ValidPassword123!";

        // Act
        var registerResult = await _driver.RegisterUser(invalidEmail, password);

        // Assert
        registerResult.Should().BeNull();
    }

    [Theory]
    [InlineData("short")] // Too short
    [InlineData("nouppercase123!")] // No uppercase
    [InlineData("NOLOWERCASE123!")] // No lowercase
    [InlineData("NoSpecialChars123")] // No special chars
    [InlineData("NoNumbers!")] // No numbers
    [InlineData("")] // Empty
    public async Task WhenAUserRegistersWithAnInvalidPasswordRegistrationShouldFail(string invalidPassword)
    {
        // Arrange
        var emailAddress = $"{Guid.NewGuid()}@test.com";

        // Act
        var registerResult = await _driver.RegisterUser(emailAddress, invalidPassword);

        // Assert
        registerResult.Should().BeNull();
    }

    [Theory]
    [InlineData("test+tag")] // Gmail-style tags
    [InlineData("test.email")] // Dots in local part
    [InlineData("email-with-hyphen")] // Hyphens
    [InlineData("email_with_underscore")] // Underscores
    public async Task WhenAUserUsesASpecialEmailFormatRegistrationShouldBeSuccessful(string email)
    {
        var emailUnderTest = $"{email}{Guid.NewGuid()}@example.com";
        // Arrange
        var password = "ValidPassword123!";

        // Act
        var registerResult = await _driver.RegisterUser(emailUnderTest, password);

        // Assert
        registerResult.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenAUserTriesToAccessAnotherUsersDataThenShouldReturn403()
    {
        // Arrange
        var emailAddress = $"{Guid.NewGuid()}@test.com";
        var password = $"{Guid.NewGuid()}!A23";
        var differentUserId = "this is not a real user id";

        // Act
        var registerResult = await _driver.RegisterUser(emailAddress, password);
        registerResult.Should().NotBeNull();
        
        var loginResponse = await _driver.Login(emailAddress, password);
        loginResponse.Should().NotBeNull();
        
        var didReturnSuccessStatusCode = await _driver.TryAccessAnotherUsersData(loginResponse!.AuthToken, differentUserId);

        // Assert
        didReturnSuccessStatusCode.Should().BeFalse();
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}