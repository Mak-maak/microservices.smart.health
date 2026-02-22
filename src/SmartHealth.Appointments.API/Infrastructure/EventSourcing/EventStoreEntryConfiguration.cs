using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartHealth.Appointments.Infrastructure.EventSourcing;

internal sealed class EventStoreEntryConfiguration : IEntityTypeConfiguration<EventStoreEntry>
{
    public void Configure(EntityTypeBuilder<EventStoreEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AggregateType).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Payload).IsRequired();
        builder.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();
    }
}
