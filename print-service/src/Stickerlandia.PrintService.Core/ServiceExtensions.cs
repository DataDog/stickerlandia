/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.PrintService.Core.DeletePrinter;
using Stickerlandia.PrintService.Core.GetPrinters;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddStickerlandiaUserManagement(this IServiceCollection services)
    {
        // Observability
        services.AddSingleton<PrintJobInstrumentation>();

        // Query handlers
        services.AddTransient<GetPrintersForEventQueryHandler>();
        services.AddTransient<GetDistinctEventsQueryHandler>();
        services.AddTransient<GetPrinterStatusesQueryHandler>();
        services.AddTransient<GetPrintJobsForPrinterQueryHandler>();

        // Command handlers — concrete implementations
        services.AddScoped<RegisterPrinterCommandHandler>();
        services.AddScoped<SubmitPrintJobCommandHandler>();
        services.AddScoped<AcknowledgePrintJobCommandHandler>();
        services.AddScoped<DeletePrinterCommandHandler>();
        services.AddScoped<DeleteEventCommandHandler>();

        // Command handler interfaces — decorated with UnitOfWork
        services.AddScoped<ICommandHandler<RegisterPrinterCommand, RegisterPrinterResponse>>(sp =>
            new UnitOfWorkCommandHandler<RegisterPrinterCommand, RegisterPrinterResponse>(
                sp.GetRequiredService<RegisterPrinterCommandHandler>(),
                sp.GetRequiredService<IUnitOfWork>()));
        services.AddScoped<ICommandHandler<SubmitPrintJobCommand, SubmitPrintJobResponse>>(sp =>
            new UnitOfWorkCommandHandler<SubmitPrintJobCommand, SubmitPrintJobResponse>(
                sp.GetRequiredService<SubmitPrintJobCommandHandler>(),
                sp.GetRequiredService<IUnitOfWork>()));
        services.AddScoped<ICommandHandler<AcknowledgePrintJobCommand, AcknowledgePrintJobResponse>>(sp =>
            new UnitOfWorkCommandHandler<AcknowledgePrintJobCommand, AcknowledgePrintJobResponse>(
                sp.GetRequiredService<AcknowledgePrintJobCommandHandler>(),
                sp.GetRequiredService<IUnitOfWork>()));
        services.AddScoped<ICommandHandler<DeletePrinterCommand>>(sp =>
            new UnitOfWorkCommandHandler<DeletePrinterCommand>(
                sp.GetRequiredService<DeletePrinterCommandHandler>(),
                sp.GetRequiredService<IUnitOfWork>()));
        services.AddScoped<ICommandHandler<DeleteEventCommand, DeleteEventResponse>>(sp =>
            new UnitOfWorkCommandHandler<DeleteEventCommand, DeleteEventResponse>(
                sp.GetRequiredService<DeleteEventCommandHandler>(),
                sp.GetRequiredService<IUnitOfWork>()));

        services.AddTransient<OutboxProcessor>();

        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();

        return services;
    }
}