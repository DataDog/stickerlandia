/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Agnostic;

#pragma warning disable CA2007

namespace Stickerlandia.UserManagement.UnitTest;

public class KafkaStickerPrintedWorkerTests
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IKafkaConsumerFactory _factory;
    private readonly KafkaStickerPrintedWorker _worker;

    public KafkaStickerPrintedWorkerTests()
    {
        _consumer = A.Fake<IConsumer<string, string>>();
        _factory = A.Fake<IKafkaConsumerFactory>();
        A.CallTo(() => _factory.Create(A<ConsumerConfig>.Ignored)).Returns(_consumer);

        _worker = new KafkaStickerPrintedWorker(
            A.Fake<ILogger<KafkaStickerPrintedWorker>>(),
            A.Fake<IServiceScopeFactory>(),
            new ConsumerConfig { BootstrapServers = "localhost:9092", GroupId = "test" },
            new ProducerConfig { BootstrapServers = "localhost:9092" },
            _factory);
    }

    [Fact]
    public async Task StartAsync_CreatesConsumerAndSubscribesToTopic()
    {
        await _worker.StartAsync();

        A.CallTo(() => _factory.Create(A<ConsumerConfig>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _consumer.Subscribe("printJobs.completed.v1")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PollAsync_UsesExistingConsumerWithoutRecreating()
    {
        A.CallTo(() => _consumer.Consume(A<TimeSpan>.Ignored))
            .Returns(null!);

        await _worker.StartAsync();
        await _worker.PollAsync(CancellationToken.None);
        await _worker.PollAsync(CancellationToken.None);

        // Consumer is only created once across multiple polls
        A.CallTo(() => _factory.Create(A<ConsumerConfig>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _consumer.Consume(A<TimeSpan>.Ignored)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task StopAsync_ClosesConsumer()
    {
        await _worker.StartAsync();
        await _worker.StopAsync(CancellationToken.None);

        A.CallTo(() => _consumer.Close()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PollAsync_WhenNoMessage_DoesNotCommit()
    {
        A.CallTo(() => _consumer.Consume(A<TimeSpan>.Ignored))
            .Returns(null!);

        await _worker.StartAsync();
        await _worker.PollAsync(CancellationToken.None);

        A.CallTo(() => _consumer.Commit(A<ConsumeResult<string, string>>.Ignored))
            .MustNotHaveHappened();
    }
}

public class KafakStickerClaimedWorkerTests
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IKafkaConsumerFactory _factory;
    private readonly KafakStickerClaimedWorker _worker;

    public KafakStickerClaimedWorkerTests()
    {
        _consumer = A.Fake<IConsumer<string, string>>();
        _factory = A.Fake<IKafkaConsumerFactory>();
        A.CallTo(() => _factory.Create(A<ConsumerConfig>.Ignored)).Returns(_consumer);

        _worker = new KafakStickerClaimedWorker(
            A.Fake<ILogger<KafakStickerClaimedWorker>>(),
            A.Fake<IServiceScopeFactory>(),
            new ConsumerConfig { BootstrapServers = "localhost:9092", GroupId = "test" },
            new ProducerConfig { BootstrapServers = "localhost:9092" },
            _factory);
    }

    [Fact]
    public async Task StartAsync_CreatesConsumerAndSubscribesToTopic()
    {
        await _worker.StartAsync();

        A.CallTo(() => _factory.Create(A<ConsumerConfig>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _consumer.Subscribe("users.stickerClaimed.v1")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PollAsync_UsesExistingConsumerWithoutRecreating()
    {
        A.CallTo(() => _consumer.Consume(A<TimeSpan>.Ignored))
            .Returns(null!);

        await _worker.StartAsync();
        await _worker.PollAsync(CancellationToken.None);
        await _worker.PollAsync(CancellationToken.None);

        // Consumer is only created once across multiple polls
        A.CallTo(() => _factory.Create(A<ConsumerConfig>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _consumer.Consume(A<TimeSpan>.Ignored)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task StopAsync_ClosesConsumer()
    {
        await _worker.StartAsync();
        await _worker.StopAsync(CancellationToken.None);

        A.CallTo(() => _consumer.Close()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PollAsync_WhenNoMessage_DoesNotCommit()
    {
        A.CallTo(() => _consumer.Consume(A<TimeSpan>.Ignored))
            .Returns(null!);

        await _worker.StartAsync();
        await _worker.PollAsync(CancellationToken.None);

        A.CallTo(() => _consumer.Commit(A<ConsumeResult<string, string>>.Ignored))
            .MustNotHaveHappened();
    }
}
