/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class PrintJobDtoTests
{
    [Fact]
    public void FromPrintJobShouldMapAllFields()
    {
        // Arrange
        var printerId = new PrinterId("EVENT-PRINTER");
        var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");

        // Act
        var dto = PrintJobDto.FromPrintJob(printJob);

        // Assert
        dto.PrintJobId.Should().Be(printJob.Id.Value);
        dto.UserId.Should().Be("user123");
        dto.StickerId.Should().Be("sticker456");
        dto.StickerUrl.Should().Be("https://example.com/sticker.png");
        dto.CreatedAt.Should().Be(printJob.CreatedAt);
    }

    [Fact]
    public void FromPrintJobWithNullShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => PrintJobDto.FromPrintJob(null!);
        action.Should().Throw<ArgumentNullException>();
    }
}
