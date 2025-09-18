/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Stickerlandia.UserManagement.Agnostic;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresUserRepository(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<UserManagementDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("database"), 
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("Stickerlandia.UserManagement.Agnostic"));
        });

        // Register repositories
        

        return services;
    }
}
