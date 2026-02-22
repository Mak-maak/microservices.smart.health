namespace SmartHealth.Appointments.Infrastructure.EventSourcing;

/// <summary>
/// Single row in the event store.  Used when the EventSourcing feature flag is enabled.
/// </summary>
public sealed class EventStoreEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The aggregate (e.g. Appointment) ID.</summary>
    public Guid AggregateId { get; set; }

    /// <summary>Aggregate type name (e.g. "Appointment").</summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>Fully-qualified event type name.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>JSON-serialised event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Monotonically increasing version within the aggregate stream.</summary>
    public int Version { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
