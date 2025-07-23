// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class MockMessaging : IMessaging
{
    public Task SendMessageAsync(string queueName, object message)
    {
        // Mock implementation - just simulate successful message sending
        return Task.CompletedTask;
    }
}