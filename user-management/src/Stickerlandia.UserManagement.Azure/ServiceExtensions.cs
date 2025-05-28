// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Auth;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Azure;

public static class ServiceExtensions
{
    public static IServiceCollection AddAzureAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresAuthServices(configuration);

        services.AddSingleton<IMessagingWorker, ServiceBusStickerClaimedWorker>();
        services.AddSingleton(new ServiceBusClient(configuration["ConnectionStrings:messaging"]));

        services.AddSingleton<IUserEventPublisher, ServiceBusEventPublisher>();

        return services;
    }
}