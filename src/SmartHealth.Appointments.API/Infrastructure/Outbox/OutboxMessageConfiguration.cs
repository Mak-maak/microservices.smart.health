using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartHealth.Appointments.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.MessageType).HasMaxLength(500).IsRequired();
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.CorrelationId).HasMaxLength(100);

        // Index to efficiently poll un-processed messages
        builder.HasIndex(o => o.ProcessedAt)
            .HasFilter("[ProcessedAt] IS NULL");
    }
}
