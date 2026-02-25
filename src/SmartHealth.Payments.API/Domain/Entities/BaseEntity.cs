namespace SmartHealth.Payments.Domain.Entities;

/// <summary>
/// Base entity providing identity, concurrency token, and domain event collection.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>EF Core optimistic-concurrency token (row version).</summary>
    public byte[]? RowVersion { get; set; }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(object domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
