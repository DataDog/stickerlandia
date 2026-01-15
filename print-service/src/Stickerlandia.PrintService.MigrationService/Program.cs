/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.MigrationService;
using Stickerlandia.PrintService.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
//builder.AddServiceDefaults();
builder.Services.ConfigureDefaultPrintServices(builder.Configuration, false);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();