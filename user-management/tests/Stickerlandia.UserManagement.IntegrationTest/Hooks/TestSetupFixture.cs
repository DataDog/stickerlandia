// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;

namespace Stickerlandia.UserManagement.IntegrationTest.Hooks;

public class TestSetupFixture : IDisposable
{
    private string BaseUrl = $"{TestConstants.DefaultTestUrl}/api";
    public readonly IMessaging Messaging;
    public readonly HttpClient HttpClient;
    public readonly DistributedApplication? App;

    public TestSetupFixture()
    {
        var shouldTestAgainstRealResources = Environment.GetEnvironmentVariable("TEST_REAL_RESOURCES") == "true";

        if (!shouldTestAgainstRealResources)
        {
            // Run all local resources with Asipre for testing
            var builder = DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Stickerlandia_UserManagement_Aspire>().GetAwaiter().GetResult();

            App = builder.BuildAsync().GetAwaiter().GetResult();

            App.StartAsync().GetAwaiter().GetResult();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            App.ResourceNotifications.WaitForResourceHealthyAsync(
                    "api",
                    cts.Token)
                .GetAwaiter().GetResult();

            HttpClient = App.CreateHttpClient("api");

            var messagingConnectionString = App.GetConnectionStringAsync("messaging").GetAwaiter().GetResult();
            
            if (string.IsNullOrEmpty(messagingConnectionString))
            {
                throw new Exception("Messaging connection string is not set.");
            }

            Messaging = new AzureServiceBusMessaging(messagingConnectionString);
        }
        else
        {
            Messaging = new AzureServiceBusMessaging(TestConstants.DefaultMessagingConnection);
            HttpClient = new HttpClient();
        }
    }

    public void Dispose()
    {
        App.StopAsync().GetAwaiter().GetResult();
    }
}