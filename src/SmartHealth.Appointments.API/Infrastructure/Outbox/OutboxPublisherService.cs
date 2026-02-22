using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.Outbox;

/// <summary>
/// Background service that polls the Outbox table and publishes pending messages
/// to Azure Service Bus via MassTransit.
///
/// Design decisions:
///   – Runs on a configurable polling interval (default 5 s).
///   – Retries up to MaxRetries before moving to dead-letter state.
///   – Uses a short-lived DI scope per batch to avoid long-lived DbContexts.
/// </summary>
public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in outbox publisher loop.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var msg in pending)
        {
            try
            {
                var type = Type.GetType(msg.MessageType);
                if (type is null)
                {
                    logger.LogWarning("Unknown message type {Type}; skipping.", msg.MessageType);
                    msg.ProcessedAt = DateTime.UtcNow; // mark as processed to avoid looping
                    continue;
                }

                var payload = JsonSerializer.Deserialize(msg.Payload, type);
                if (payload is not null)
                {
                    await publishEndpoint.Publish(payload, type, ct);
                }

                msg.ProcessedAt = DateTime.UtcNow;
                logger.LogInformation("Outbox message {Id} published.", msg.Id);
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                logger.LogError(ex, "Failed to publish outbox message {Id} (attempt {Attempt}).",
                    msg.Id, msg.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
