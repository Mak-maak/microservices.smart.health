using MassTransit;

namespace SmartHealth.Appointments.Infrastructure.Saga;

/// <summary>
/// MassTransit saga state persisted via EF Core.
/// CorrelationId is the AppointmentId.
/// </summary>
public sealed class AppointmentSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime SlotStartTime { get; set; }
    public DateTime SlotEndTime { get; set; }

    /// <summary>Optimistic concurrency token managed by MassTransit.</summary>
    public byte[]? RowVersion { get; set; }
}
