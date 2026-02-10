/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.GetPrinters;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class GetPrintersForEventQueryHandlerTests
{
    private readonly IPrinterRepository _repository;
    private readonly GetPrintersForEventQueryHandler _handler;

    public GetPrintersForEventQueryHandlerTests()
    {
        _repository = A.Fake<IPrinterRepository>();
        _handler = new GetPrintersForEventQueryHandler(_repository);
    }

    public class HandleMethod : GetPrintersForEventQueryHandlerTests
    {
        [Fact]
        public async Task WithValidQuery_ReturnsPrinterDTOs()
        {
            var query = new GetPrintersForEventQuery("TestEvent");
            var printers = new List<Printer>
            {
                Printer.From(new PrinterId("TESTEVENT-PRINTER1"), "TestEvent", "Printer1", "key1"),
                Printer.From(new PrinterId("TESTEVENT-PRINTER2"), "TestEvent", "Printer2", "key2")
            };
            A.CallTo(() => _repository.GetPrintersForEventAsync("TestEvent"))
                .Returns(printers);

            var result = await _handler.Handle(query);

            result.Should().HaveCount(2);
            result[0].PrinterId.Should().Be("TESTEVENT-PRINTER1");
            result[0].PrinterName.Should().Be("Printer1");
            result[1].PrinterId.Should().Be("TESTEVENT-PRINTER2");
            result[1].PrinterName.Should().Be("Printer2");
        }

        [Fact]
        public async Task WithNoMatchingPrinters_ReturnsEmptyList()
        {
            var query = new GetPrintersForEventQuery("NonExistentEvent");
            A.CallTo(() => _repository.GetPrintersForEventAsync("NonExistentEvent"))
                .Returns(new List<Printer>());

            var result = await _handler.Handle(query);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task WithNullQuery_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WithNullEventName_ThrowsArgumentException()
        {
            var query = new GetPrintersForEventQuery(null!);

            var action = async () => await _handler.Handle(query);

            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Invalid auth token");
        }

        [Fact]
        public async Task WithValidQuery_MapsAllPropertiesCorrectly()
        {
            var query = new GetPrintersForEventQuery("MyEvent");
            var printers = new List<Printer>
            {
                Printer.From(new PrinterId("MYEVENT-MYPRINTER"), "MyEvent", "MyPrinter", "secretKey")
            };
            A.CallTo(() => _repository.GetPrintersForEventAsync("MyEvent"))
                .Returns(printers);

            var result = await _handler.Handle(query);

            result.Should().HaveCount(1);
            result[0].PrinterId.Should().Be("MYEVENT-MYPRINTER");
            result[0].EventName.Should().Be("MyEvent");
            result[0].PrinterName.Should().Be("MyPrinter");
        }

        [Fact]
        public async Task WithValidQuery_DoesNotExposeKeyInDTO()
        {
            var query = new GetPrintersForEventQuery("TestEvent");
            var printers = new List<Printer>
            {
                Printer.From(new PrinterId("TESTEVENT-PRINTER"), "TestEvent", "Printer", "secret-key-123")
            };
            A.CallTo(() => _repository.GetPrintersForEventAsync("TestEvent"))
                .Returns(printers);

            var result = await _handler.Handle(query);

            result.Should().HaveCount(1);
            var dto = result[0];
            dto.Should().BeOfType<PrinterDTO>();
            // PrinterDTO does not have a Key property - it's excluded
        }
    }
}
