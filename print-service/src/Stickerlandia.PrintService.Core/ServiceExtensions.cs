/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.PrintService.Core.GetPrinters;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddStickerlandiaUserManagement(this IServiceCollection services)
    {
        services.AddTransient<RegisterPrinterCommandHandler>();
        services.AddTransient<GetPrintersForEventQueryHandler>();
        services.AddTransient<GetPrinterStatusesQueryHandler>();
        services.AddTransient<SubmitPrintJobCommandHandler>();
        services.AddTransient<GetPrintJobsForPrinterQueryHandler>();
        services.AddTransient<AcknowledgePrintJobCommandHandler>();

        services.AddTransient<OutboxProcessor>();

        return services;
    }
}