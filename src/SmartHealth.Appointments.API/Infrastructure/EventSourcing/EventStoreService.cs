using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.EventSourcing;

/// <summary>
/// Stores domain events instead of – or alongside – entity state.
/// Enabled via the "Features:EventSourcing" configuration flag.
///
/// Supports:
///   – Append-only event storage with per-aggregate versioning.
///   – Aggregate rehydration by replaying events.
///   – Snapshot support (saves current state as a snapshot event at configurable intervals).
/// </summary>
public sealed class EventStoreService(AppointmentsDbContext db)
{
    private const int SnapshotThreshold = 50;

    public async Task AppendEventsAsync(BaseEntity aggregate, CancellationToken ct = default)
    {
        var aggregateType = aggregate.GetType().Name;

        int currentVersion = await db.EventStoreEntries
            .Where(e => e.AggregateId == aggregate.Id)
            .MaxAsync(e => (int?)e.Version, ct) ?? 0;

        foreach (var domainEvent in aggregate.DomainEvents)
        {
            currentVersion++;
            db.EventStoreEntries.Add(new EventStoreEntry
            {
                AggregateId = aggregate.Id,
                AggregateType = aggregateType,
                EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                Version = currentVersion,
                OccurredAt = DateTime.UtcNow
            });
        }

        // Snapshot every N events
        if (currentVersion % SnapshotThreshold == 0)
        {
            db.EventStoreEntries.Add(new EventStoreEntry
            {
                AggregateId = aggregate.Id,
                AggregateType = aggregateType,
                EventType = $"__Snapshot__{aggregateType}",
                Payload = JsonSerializer.Serialize(aggregate, aggregate.GetType()),
                Version = currentVersion,
                OccurredAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Returns all raw event entries for the given aggregate (for replay or audit).
    /// </summary>
    public async Task<IReadOnlyList<EventStoreEntry>> GetEventsAsync(
        Guid aggregateId, CancellationToken ct = default)
        => await db.EventStoreEntries
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);
}
