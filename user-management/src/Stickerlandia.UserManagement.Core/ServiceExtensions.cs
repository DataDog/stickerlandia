// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Core.Auth;
using Stickerlandia.UserManagement.Core.GetUserDetails;
using Stickerlandia.UserManagement.Core.Login;
using Stickerlandia.UserManagement.Core.Outbox;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;
using Stickerlandia.UserManagement.Core.UpdateUserDetails;

namespace Stickerlandia.UserManagement.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddStickerlandiaUserManagement(this IServiceCollection services)
    {
        services.AddTransient<UpdateUserDetailsHandler>();
        services.AddTransient<RegisterCommandHandler>();
        services.AddTransient<LoginCommandHandler>();
        services.AddTransient<GetUserDetailsQueryHandler>();
        services.AddTransient<StickerClaimedEventHandler>();
        
        services.AddSingleton<IAuthService, JwtAuthService>();
        services.AddTransient<OutboxProcessor>();
        
        return services;
    }
}