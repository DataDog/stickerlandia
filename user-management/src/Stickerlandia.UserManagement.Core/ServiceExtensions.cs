// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Core.Auth;
using Stickerlandia.UserManagement.Core.GetUserDetails;
using Stickerlandia.UserManagement.Core.Login;
using Stickerlandia.UserManagement.Core.Register;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddStickerlandiaUserManagement(this IServiceCollection services)
    {
        services.AddSingleton<RegisterCommandHandler>();
        services.AddSingleton<LoginCommandHandler>();
        services.AddSingleton<StickerClaimedEventHandler>();
        services.AddSingleton<GetUserDetailsQueryHandler>();
        services.AddSingleton<IAuthService, JwtAuthService>();
        
        return services;
    }
}