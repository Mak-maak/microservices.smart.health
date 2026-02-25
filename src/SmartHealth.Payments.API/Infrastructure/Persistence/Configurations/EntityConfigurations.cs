using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHealth.Payments.Domain.Entities;
using SmartHealth.Payments.Infrastructure.Outbox;

namespace SmartHealth.Payments.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.AppointmentId).IsRequired();
        builder.Property(p => p.UserId).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.StripePaymentIntentId).HasMaxLength(255);
        builder.Property(p => p.FailureReason).HasMaxLength(1000);
        builder.Property(p => p.RowVersion).IsRowVersion();

        // Unique index on AppointmentId ensures idempotency
        builder.HasIndex(p => p.AppointmentId)
            .IsUnique()
            .HasDatabaseName("IX_Payments_AppointmentId");
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.MessageType).HasMaxLength(500).IsRequired();
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.CorrelationId).HasMaxLength(100);
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.RetryCount).IsRequired();

        builder.HasIndex(o => new { o.ProcessedAt, o.RetryCount, o.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Pending");
    }
}
