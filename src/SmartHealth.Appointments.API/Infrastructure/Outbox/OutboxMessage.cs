namespace SmartHealth.Appointments.Infrastructure.Outbox;

/// <summary>
/// Outbox message persisted in the same transaction as the domain entity changes.
/// A background publisher reads pending messages and publishes them to Azure Service Bus.
/// This guarantees at-least-once delivery without the dual-write problem.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Fully-qualified type name of the message payload.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>JSON-serialised payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Correlation / causation ID for distributed tracing.</summary>
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Number of publish attempts (for retry / dead-letter logic).</summary>
    public int RetryCount { get; set; }

    public bool IsProcessed => ProcessedAt.HasValue;
}
