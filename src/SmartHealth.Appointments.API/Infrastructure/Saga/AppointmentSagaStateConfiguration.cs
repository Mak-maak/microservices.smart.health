using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.Saga;

/// <summary>EF Core configuration for saga state persistence.</summary>
internal sealed class AppointmentSagaStateConfiguration
    : SagaClassMap<AppointmentSagaState>
{
    protected override void Configure(EntityTypeBuilder<AppointmentSagaState> entity,
        ModelBuilder model)
    {
        entity.HasKey(s => s.CorrelationId);
        entity.Property(s => s.CurrentState).HasMaxLength(64).IsRequired();
        entity.Property(s => s.RowVersion).IsRowVersion();
    }
}
