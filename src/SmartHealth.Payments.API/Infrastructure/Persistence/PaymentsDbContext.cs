using Microsoft.EntityFrameworkCore;
using SmartHealth.Payments.Domain.Entities;
using SmartHealth.Payments.Infrastructure.Messaging;
using SmartHealth.Payments.Infrastructure.Outbox;
using System.Text.Json;

namespace SmartHealth.Payments.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext for the Payments microservice.
/// Automatically populates OutboxMessages from domain events on save.
/// </summary>
public sealed class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }

    /// <summary>
    /// Overrides SaveChangesAsync to automatically populate OutboxMessages
    /// from domain events before persisting changes.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                // Only outbox events that represent integration events (PaymentCompleted, PaymentFailed)
                if (domainEvent is Domain.Events.PaymentCompletedEvent completedEvent)
                {
                    var integrationEvent = new PaymentCompletedIntegrationEvent(
                        completedEvent.PaymentId,
                        completedEvent.AppointmentId,
                        "Completed",
                        completedEvent.TransactionId);

                    var integrationType = integrationEvent.GetType();
                    OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        MessageType = integrationType.AssemblyQualifiedName ?? integrationType.FullName!,
                        Payload = JsonSerializer.Serialize(integrationEvent, integrationType),
                        CorrelationId = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else if (domainEvent is Domain.Events.PaymentFailedEvent failedEvent)
                {
                    var integrationEvent = new PaymentFailedIntegrationEvent(
                        failedEvent.PaymentId,
                        failedEvent.AppointmentId,
                        failedEvent.Reason);

                    var integrationType = integrationEvent.GetType();
                    OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        MessageType = integrationType.AssemblyQualifiedName ?? integrationType.FullName!,
                        Payload = JsonSerializer.Serialize(integrationEvent, integrationType),
                        CorrelationId = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            entity.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();
}
