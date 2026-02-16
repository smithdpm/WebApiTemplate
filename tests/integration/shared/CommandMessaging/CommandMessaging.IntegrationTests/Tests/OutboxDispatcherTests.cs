using Cqrs.BackgroundServices;
using Cqrs.IntegrationTests.Fixtures;
using Cqrs.IntegrationTests.Fixtures.CollectionFixtures;
using Cqrs.Outbox;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;

namespace Cqrs.IntegrationTests.Tests;

[Collection(nameof(OutboxRepositoryCollection))]
public class OutboxDispatcherTests
{
    private FakeLogger<OutboxDispatcherService> _fakeLogger;
    private readonly IOutboxDispatcher _dispatcherMock;
    private readonly IHost _host;

    public OutboxDispatcherTests(OutboxRepositoryFixture outboxRepositoryFixture)
    {
        _fakeLogger = new FakeLogger<OutboxDispatcherService>();
        _dispatcherMock = Substitute.For<IOutboxDispatcher>();


        _host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddScoped(_ => _dispatcherMock);
            services.AddHostedService<OutboxDispatcherService>();
            services.AddSingleton<ILogger<OutboxDispatcherService>>(_ => _fakeLogger);
        })
        .Build();
    }

    [Fact]
    public async Task BackgroundService_ShouldPollContinuously()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _host.StartAsync(cancellationToken);
        await Task.Delay(500, cancellationToken);

        // Assert
        await _dispatcherMock.Received(Quantity.Within(5, int.MaxValue))
            .ExecuteAsync(Arg.Any<CancellationToken>());
        await _host.StopAsync(cancellationToken);
    }


    [Fact]
    public async Task BackgroundService_ShouldHandleDispatcherExceptions()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        _dispatcherMock.ExecuteAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Test dispatcher error")));


        // Act
        await _host.StartAsync(cancellationToken);
        await Task.Delay(500, cancellationToken);

        // Assert
        var log = _fakeLogger.Collector.LatestRecord;
        log.Level.ShouldBe(LogLevel.Error);
        log.Message.ShouldContain("Error processing Outbox");
        log.Exception!.Message.ShouldBe("Test dispatcher error");
        await _host.StopAsync(cancellationToken);
    }
}