// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.GetPrinters;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class GetDistinctEventsQueryHandlerTests
{
    private readonly IPrinterRepository _repository;
    private readonly GetDistinctEventsQueryHandler _handler;

    public GetDistinctEventsQueryHandlerTests()
    {
        _repository = A.Fake<IPrinterRepository>();
        _handler = new GetDistinctEventsQueryHandler(_repository);
    }

    public class HandleMethod : GetDistinctEventsQueryHandlerTests
    {
        [Fact]
        public async Task WithValidQuery_ReturnsDistinctEventNames()
        {
            var events = new List<string> { "EventA", "EventB", "EventC" };
            A.CallTo(() => _repository.GetDistinctEventNamesAsync())
                .Returns(events);

            var result = await _handler.Handle(new GetDistinctEventsQuery());

            result.Should().HaveCount(3);
            result.Should().ContainInOrder("EventA", "EventB", "EventC");
        }

        [Fact]
        public async Task WithNoEvents_ReturnsEmptyList()
        {
            A.CallTo(() => _repository.GetDistinctEventNamesAsync())
                .Returns(new List<string>());

            var result = await _handler.Handle(new GetDistinctEventsQuery());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task WithNullQuery_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CallsRepositoryOnce()
        {
            A.CallTo(() => _repository.GetDistinctEventNamesAsync())
                .Returns(new List<string> { "Event1" });

            await _handler.Handle(new GetDistinctEventsQuery());

            A.CallTo(() => _repository.GetDistinctEventNamesAsync())
                .MustHaveHappenedOnceExactly();
        }
    }
}