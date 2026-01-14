/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using FakeItEasy;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class GetPrintJobsForPrinterQueryHandlerTests
{
    private readonly IPrintJobRepository _printJobRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly GetPrintJobsForPrinterQueryHandler _handler;

    public GetPrintJobsForPrinterQueryHandlerTests()
    {
        _printJobRepository = A.Fake<IPrintJobRepository>();
        _printerRepository = A.Fake<IPrinterRepository>();
        _handler = new GetPrintJobsForPrinterQueryHandler(_printJobRepository, _printerRepository);
    }

    [Fact]
    public async Task HandleWithValidQueryShouldReturnJobs()
    {
        // Arrange
        var printerId = new PrinterId("EVENT-PRINTER");
        var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");

        A.CallTo(() => _printJobRepository.GetQueuedJobsForPrinterAsync(printerId.Value, 10))
            .Returns(new List<PrintJob> { printJob });

        var query = new GetPrintJobsForPrinterQuery
        {
            PrinterId = printerId.Value,
            MaxJobs = 10
        };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Jobs.Should().HaveCount(1);
        result.Jobs[0].PrintJobId.Should().Be(printJob.Id.Value);
        result.Jobs[0].UserId.Should().Be("user123");
        result.Jobs[0].StickerId.Should().Be("sticker456");
    }

    [Fact]
    public async Task HandleShouldUpdatePrinterHeartbeat()
    {
        // Arrange
        var printerId = "EVENT-PRINTER";
        A.CallTo(() => _printJobRepository.GetQueuedJobsForPrinterAsync(printerId, 10))
            .Returns(new List<PrintJob>());

        var query = new GetPrintJobsForPrinterQuery
        {
            PrinterId = printerId,
            MaxJobs = 10
        };

        // Act
        await _handler.Handle(query);

        // Assert
        A.CallTo(() => _printerRepository.UpdateHeartbeatAsync(printerId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task HandleWithNoJobsShouldReturnEmptyList()
    {
        // Arrange
        var printerId = "EVENT-PRINTER";
        A.CallTo(() => _printJobRepository.GetQueuedJobsForPrinterAsync(printerId, 10))
            .Returns(new List<PrintJob>());

        var query = new GetPrintJobsForPrinterQuery
        {
            PrinterId = printerId,
            MaxJobs = 10
        };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleWithMultipleJobsShouldReturnAllJobs()
    {
        // Arrange
        var printerId = new PrinterId("EVENT-PRINTER");
        var job1 = PrintJob.Create(printerId, "user1", "sticker1", "https://example.com/1.png");
        var job2 = PrintJob.Create(printerId, "user2", "sticker2", "https://example.com/2.png");
        var job3 = PrintJob.Create(printerId, "user3", "sticker3", "https://example.com/3.png");

        A.CallTo(() => _printJobRepository.GetQueuedJobsForPrinterAsync(printerId.Value, 10))
            .Returns(new List<PrintJob> { job1, job2, job3 });

        var query = new GetPrintJobsForPrinterQuery
        {
            PrinterId = printerId.Value,
            MaxJobs = 10
        };

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.Jobs.Should().HaveCount(3);
    }

    [Fact]
    public async Task HandleShouldRespectMaxJobsParameter()
    {
        // Arrange
        var printerId = "EVENT-PRINTER";
        var query = new GetPrintJobsForPrinterQuery
        {
            PrinterId = printerId,
            MaxJobs = 5
        };

        A.CallTo(() => _printJobRepository.GetQueuedJobsForPrinterAsync(printerId, 5))
            .Returns(new List<PrintJob>());

        // Act
        await _handler.Handle(query);

        // Assert
        A.CallTo(() => _printJobRepository.GetQueuedJobsForPrinterAsync(printerId, 5))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task HandleWithNullQueryShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!));
    }

    [Fact]
    public async Task HandleWithEmptyPrinterIdShouldThrowArgumentException()
    {
        // Arrange
        var query = new GetPrintJobsForPrinterQuery
        {
            PrinterId = "",
            MaxJobs = 10
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(query));
    }
}
