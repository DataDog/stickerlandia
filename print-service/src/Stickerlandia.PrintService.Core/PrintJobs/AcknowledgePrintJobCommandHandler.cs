// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Handler for acknowledging print job completion or failure.
/// </summary>
public class AcknowledgePrintJobCommandHandler(
    IPrintJobRepository printJobRepository,
    IPrinterRepository printerRepository,
    IOutbox outbox,
    PrintJobInstrumentation instrumentation)
    : ICommandHandler<AcknowledgePrintJobCommand, AcknowledgePrintJobResponse>
{
    public async Task<AcknowledgePrintJobResponse> Handle(AcknowledgePrintJobCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.IsValid())
        {
            throw new InvalidPrintJobException("Invalid acknowledgment command");
        }

        using var activity = PrintJobInstrumentation.StartAcknowledgeJobActivity(command.PrintJobId, command.Success);

        try
        {
            // Load the print job
            var printJob = await printJobRepository.GetByIdAsync(command.PrintJobId);

            if (printJob is null)
            {
                throw new PrintJobNotFoundException($"Print job '{command.PrintJobId}' not found");
            }

            // Verify the job belongs to the printer making the request
            if (printJob.PrinterId.Value != command.PrinterId)
            {
                throw new PrintJobOwnershipException($"Print job '{command.PrintJobId}' does not belong to printer '{command.PrinterId}'");
            }

            // Verify the job is in Processing status
            if (printJob.Status != PrintJobStatus.Processing)
            {
                throw new PrintJobStatusException($"Print job '{command.PrintJobId}' is not in Processing status (current: {printJob.Status})");
            }

            // Calculate processing time
            var processingTime = DateTimeOffset.UtcNow - printJob.ProcessedAt!.Value;

            // Get printer info for metrics
            var printer = await printerRepository.GetPrinterByKeyAsync(command.PrinterId);
            var eventName = printer?.EventName ?? "unknown";

            // Update the job status
            if (command.Success)
            {
                printJob.Complete();
                await outbox.StoreEventFor(new PrintJobCompletedEvent(printJob));

                // Record completion metrics
                instrumentation.RecordJobCompleted(command.PrinterId, eventName, processingTime);
            }
            else
            {
                printJob.Fail(command.FailureReason!);
                await outbox.StoreEventFor(new PrintJobFailedEvent(printJob));

                // Record failure metrics
                instrumentation.RecordJobFailed(command.PrinterId, eventName, processingTime, command.FailureReason);
            }

            // Save the updated job
            await printJobRepository.UpdateAsync(printJob);

            // Update the printer's last job processed timestamp
            if (printer != null)
            {
                printer.RecordJobProcessed();
                await printerRepository.UpdateAsync(printer);
            }

            activity?.SetTag("print.printer_id", command.PrinterId);
            activity?.SetTag("print.event_name", eventName);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new AcknowledgePrintJobResponse { Acknowledged = true };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}

/// <summary>
/// Response from acknowledging a print job.
/// </summary>
public record AcknowledgePrintJobResponse
{
    public bool Acknowledged { get; init; }
}
