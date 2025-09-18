/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.UserManagement.ServiceDefaults;
using Stickerlandia.UserManagement.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults(enableDefaultUi: false);

builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddHostedService<StickerClaimedWorker>();

var host = builder.Build();
host.Run();