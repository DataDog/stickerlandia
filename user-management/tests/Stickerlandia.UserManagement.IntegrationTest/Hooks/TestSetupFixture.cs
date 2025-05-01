// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;
using Xunit.Abstractions;

namespace Stickerlandia.UserManagement.IntegrationTest.Hooks;

public class TestSetupFixture : IDisposable
{
    public readonly IMessaging Messaging;
    public readonly HttpClient HttpClient;
    public readonly DistributedApplication? App;

    public TestSetupFixture()
    {
        var shouldTestAgainstRealResources = Environment.GetEnvironmentVariable("TEST_REAL_RESOURCES") == "true";

        if (!shouldTestAgainstRealResources)
        {
            
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