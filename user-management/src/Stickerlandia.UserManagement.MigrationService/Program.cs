/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.MigrationService;
using Stickerlandia.UserManagement.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
//builder.AddServiceDefaults();
builder.Services.ConfigureDefaultUserManagementServices(builder.Configuration, false);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();