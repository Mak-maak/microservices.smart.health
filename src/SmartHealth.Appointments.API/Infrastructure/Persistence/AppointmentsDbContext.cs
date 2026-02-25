using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Infrastructure.EventSourcing;
using SmartHealth.Appointments.Infrastructure.Outbox;
using SmartHealth.Appointments.Infrastructure.Saga;
using System.Text.Json;

namespace SmartHealth.Appointments.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext for the Appointments microservice.
/// Supports:
///   – Domain entities (Patients, Doctors, Appointments)
///   – Outbox messages (transactional outbox pattern)
///   – Event store (optional event sourcing)
///   – MassTransit saga state (AppointmentSagaState)
///   – Automatic outbox and event store population on save
/// </summary>
public sealed class AppointmentsDbContext : DbContext
{
    private readonly IConfiguration? _configuration;

    public AppointmentsDbContext(
        DbContextOptions<AppointmentsDbContext> options,
        IConfiguration? configuration = null)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<EventStoreEntry> EventStoreEntries => Set<EventStoreEntry>();
    public DbSet<AppointmentSagaState> AppointmentSagaStates => Set<AppointmentSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppointmentsDbContext).Assembly);
    }

    /// <summary>
    /// Overrides SaveChangesAsync to automatically populate OutboxMessages and EventStoreEntries
    /// from domain events before persisting changes.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Check if event sourcing is enabled
        var eventSourcingEnabled = _configuration?.GetValue<bool>("Features:EventSourcing") ?? false;

        // Get all tracked entities that have domain events
        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        // Process domain events for each entity
        foreach (var entity in entitiesWithEvents)
        {
            var aggregateType = entity.GetType().Name;
            var aggregateId = entity.Id;

            // Get current event store version for this aggregate (if event sourcing is enabled)
            int currentVersion = 0;
            if (eventSourcingEnabled)
            {
                currentVersion = await EventStoreEntries
                    .Where(e => e.AggregateId == aggregateId)
                    .MaxAsync(e => (int?)e.Version, cancellationToken) ?? 0;
            }

            // Process each domain event
            foreach (var domainEvent in entity.DomainEvents)
            {
                var eventType = domainEvent.GetType();
                var eventTypeName = eventType.AssemblyQualifiedName ?? eventType.FullName ?? eventType.Name;
                var payload = JsonSerializer.Serialize(domainEvent, eventType);

                // 1. Create OutboxMessage for eventual publishing to message bus
                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    MessageType = eventTypeName,
                    Payload = payload,
                    CorrelationId = Guid.NewGuid().ToString(), // Can be enhanced with actual correlation ID
                    CreatedAt = DateTime.UtcNow,
                    RetryCount = 0
                };
                OutboxMessages.Add(outboxMessage);

                // 2. Create EventStoreEntry if event sourcing is enabled
                if (eventSourcingEnabled)
                {
                    currentVersion++;
                    var eventStoreEntry = new EventStoreEntry
                    {
                        Id = Guid.NewGuid(),
                        AggregateId = aggregateId,
                        AggregateType = aggregateType,
                        EventType = eventTypeName,
                        Payload = payload,
                        Version = currentVersion,
                        OccurredAt = DateTime.UtcNow
                    };
                    EventStoreEntries.Add(eventStoreEntry);
                }
            }

            // Clear domain events after processing to prevent reprocessing
            entity.ClearDomainEvents();
        }

        // Save all changes (domain entities + outbox messages + event store entries) in a single transaction
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Synchronous version - delegates to async to maintain consistency
    /// </summary>
    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }
}
