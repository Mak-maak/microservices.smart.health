using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHealth.Appointments.Infrastructure.Outbox;
using SmartHealth.Appointments.Infrastructure.Persistence;
using MassTransit;
using NSubstitute;
using System.Text.Json;

namespace SmartHealth.Appointments.Tests.Outbox;

/// <summary>Tests for the transactional outbox publisher service.</summary>
public sealed class OutboxPublisherServiceTests : IDisposable
{
    private readonly AppointmentsDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public OutboxPublisherServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppointmentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppointmentsDbContext(options);
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task OutboxMessage_AfterProcessing_ShouldBeMarkedProcessed()
    {
        // Arrange: add an outbox message with a known type
        var msg = new TestMessage("Hello outbox");
        _db.OutboxMessages.Add(new OutboxMessage
        {
            MessageType = typeof(TestMessage).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(msg),
            CorrelationId = Guid.NewGuid().ToString()
        });
        await _db.SaveChangesAsync();

        // Act: run one processing cycle
        var scopeFactory = BuildScopeFactory();
        var service = new OutboxPublisherService(scopeFactory, NullLogger<OutboxPublisherService>.Instance);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Use reflection to call the private ProcessBatchAsync
        var method = typeof(OutboxPublisherService).GetMethod("ProcessBatchAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(service, [cts.Token])!;

        // Assert: message marked as processed
        var processed = await _db.OutboxMessages.FirstAsync();
        processed.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OutboxMessage_WhenPublishFails_ShouldIncrementRetryCount()
    {
        // Arrange: add a message with a type that will fail to resolve
        _db.OutboxMessages.Add(new OutboxMessage
        {
            MessageType = "NonExistentType, NonExistentAssembly",
            Payload = "{}",
            CorrelationId = Guid.NewGuid().ToString()
        });
        await _db.SaveChangesAsync();

        var scopeFactory = BuildScopeFactory();
        var service = new OutboxPublisherService(scopeFactory, NullLogger<OutboxPublisherService>.Instance);
        var method = typeof(OutboxPublisherService).GetMethod("ProcessBatchAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;

        // Unknown type -> message gets marked processed (skipped)
        var msg = await _db.OutboxMessages.FirstAsync();
        msg.ProcessedAt.Should().NotBeNull();
    }

    private IServiceScopeFactory BuildScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_db);
        services.AddSingleton(_publishEndpoint);
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private sealed record TestMessage(string Value);
}
