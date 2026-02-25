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
///   – Handles database not ready scenarios gracefully during startup.
/// </summary>
public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StartupRetryDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher starting...");

        // Wait for database to be ready
        if (!await WaitForDatabaseAsync(stoppingToken))
        {
            logger.LogWarning("Outbox publisher could not connect to database. Service stopping.");
            return;
        }

        logger.LogInformation("Outbox publisher started successfully.");

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

        logger.LogInformation("Outbox publisher stopped.");
    }

    private async Task<bool> WaitForDatabaseAsync(CancellationToken ct)
    {
        const int maxAttempts = 6; // Wait up to 1 minute

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();

                // Try to connect and check if OutboxMessages table exists
                await db.Database.CanConnectAsync(ct);

                // Test query to ensure table exists
                _ = await db.OutboxMessages.AnyAsync(ct);

                logger.LogInformation("Database and OutboxMessages table are ready.");
                return true;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, 
                    "Database not ready (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay} seconds...",
                    attempt, maxAttempts, StartupRetryDelay.TotalSeconds);

                try
                {
                    await Task.Delay(StartupRetryDelay, ct);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Failed to connect to database after {MaxAttempts} attempts. " +
                    "Ensure migrations have been applied.", maxAttempts);
                return false;
            }
        }

        return false;
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

        if (pending.Count == 0)
        {
            return; // No messages to process
        }

        logger.LogInformation("Processing {Count} outbox messages.", pending.Count);

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
                logger.LogInformation("Outbox message {Id} published successfully.", msg.Id);
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                logger.LogError(ex, "Failed to publish outbox message {Id} (attempt {Attempt}/{MaxRetries}).",
                    msg.Id, msg.RetryCount, MaxRetries);

                if (msg.RetryCount >= MaxRetries)
                {
                    logger.LogError("Outbox message {Id} exceeded max retries. Moving to dead-letter state.", msg.Id);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
