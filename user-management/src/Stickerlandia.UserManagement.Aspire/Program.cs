/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.Extensions.Configuration;
using Stickerlandia.UserManagement.Aspire;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var configuredDrivingAdapters = builder.Configuration["DRIVING"];
if (!string.IsNullOrEmpty(configuredDrivingAdapters))
    DrivingAdapterSettings.OverrideTo(Enum.Parse<DrivingAdapter>(configuredDrivingAdapters));

var configuredDrivenAdapters = builder.Configuration["DRIVEN"];
if (!string.IsNullOrEmpty(configuredDrivenAdapters))
    DrivenAdapterSettings.OverrideTo(Enum.Parse<DrivenAdapters>(configuredDrivenAdapters));

InfrastructureResources? resources = null;

switch (DrivenAdapterSettings.DrivenAdapter)
{
    case DrivenAdapters.AZURE:
        resources = builder.WithAzureNativeServices();
        break;
    case DrivenAdapters.AGNOSTIC:
        resources = builder.WithAgnosticServices();
        break;
    case DrivenAdapters.AWS:
        resources = builder.WithAgnosticServices();
        break;
    case DrivenAdapters.GCP:
        resources = builder.WithGcpServices();
        break;
}

ArgumentNullException.ThrowIfNull(resources, nameof(resources));

switch (DrivingAdapterSettings.DrivingAdapter)
{
    case DrivingAdapter.AZURE:
        builder.WithContainerizedApi(resources)
            .WithAzureFunctions(resources);
        break;
    case DrivingAdapter.AGNOSTIC:
        builder.WithContainerizedApi(resources)
            .WithBackgroundWorker(resources);
        break;
    case DrivingAdapter.GCP:
        builder.WithGcpApi(resources)
            .WithGcpWorker(resources);
        break;
    case DrivingAdapter.AWS:
        builder.WithAwsLambda(resources);
        break;
}


builder.Build().Run();