// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;

namespace Stickerlandia.UserManagement.IntegrationTest.Hooks;

public class TestSetupFixture : IDisposable
{
    public readonly IMessaging Messaging;
    public readonly HttpClient HttpClient;
    public readonly DistributedApplication? App;
    
    private const string ApiApplicationName = "api";
    private const string DatabaseResourceName = "database";
    private const string MessagingResourceName = "messaging";

    public TestSetupFixture()
    {
        var shouldTestAgainstRealResources = Environment.GetEnvironmentVariable("TEST_REAL_RESOURCES") == "true";

        if (!shouldTestAgainstRealResources)
        {
            // Run all local resources with Asipre for testing
            var builder = DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Stickerlandia_UserManagement_Aspire>()
                .GetAwaiter()
                .GetResult();

            builder.Configuration["DRIVING"] = Environment.GetEnvironmentVariable("DRIVING") ?? "ASPNET";
            
            var drivenAdapter = Environment.GetEnvironmentVariable("DRIVEN") ?? "AGNOSTIC";
            builder.Configuration["DRIVEN"] = drivenAdapter;

            App = builder.BuildAsync().GetAwaiter().GetResult();

            App.StartAsync().GetAwaiter().GetResult();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            App.ResourceNotifications.WaitForResourceHealthyAsync(
                "api",
                cts.Token)
                .GetAwaiter().GetResult();

            // When Azure Functions is used, the API is not available immediately even when the container is healthy.
            Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();

            var messagingConnectionString = App.GetConnectionStringAsync("messaging").GetAwaiter().GetResult();

            HttpClient = App.CreateHttpClient("api");
            Messaging = MessagingProviderFactory.From(drivenAdapter, messagingConnectionString);
        }
        else
        {
            Messaging = new AzureServiceBusMessaging(TestConstants.DefaultMessagingConnection);
            HttpClient = new HttpClient()
            {
                BaseAddress = new Uri(TestConstants.DefaultTestUrl)
            };
        }
    }

    public void Dispose()
    {
        App.StopAsync().GetAwaiter().GetResult();
    }
}