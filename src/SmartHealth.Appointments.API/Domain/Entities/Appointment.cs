using SmartHealth.Appointments.Domain.Events;
using SmartHealth.Appointments.Domain.Exceptions;
using SmartHealth.Appointments.Domain.ValueObjects;

namespace SmartHealth.Appointments.Domain.Entities;

/// <summary>
/// Appointment aggregate root.
/// Business rules:
///   – Appointment must be future-dated.
///   – No double booking for the same doctor in the same time slot.
/// </summary>
public sealed class Appointment : BaseEntity
{
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public AppointmentSlot Slot { get; private set; } = null!;
    public AppointmentStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    // EF navigation (not returned to API callers directly)
    public Patient? Patient { get; private set; }
    public Doctor? Doctor { get; private set; }

    private Appointment() { }

    /// <summary>Factory – enforces future-date invariant.</summary>
    public static Appointment Book(Guid patientId, Guid doctorId,
        AppointmentSlot slot, string? reason)
    {
        if (slot.StartTime <= DateTime.UtcNow)
            throw new AppointmentInPastException("Appointment must be scheduled in the future.");

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            Slot = slot,
            Reason = reason,
            Status = AppointmentStatus.Requested
        };

        appointment.AddDomainEvent(new AppointmentRequestedEvent(
            appointment.Id, patientId, doctorId, slot.StartTime, slot.EndTime));

        return appointment;
    }

    public void Confirm()
    {
        EnsureStatus(AppointmentStatus.Requested, AppointmentStatus.SlotReserved);
        Status = AppointmentStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AppointmentConfirmedEvent(Id, PatientId, DoctorId));
    }

    public void ReserveSlot()
    {
        EnsureStatus(AppointmentStatus.Requested);
        Status = AppointmentStatus.SlotReserved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Appointment is already cancelled.");

        CancellationReason = reason;
        Status = AppointmentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AppointmentCancelledEvent(Id, PatientId, DoctorId, reason));
    }

    public void Fail(string reason)
    {
        Status = AppointmentStatus.Failed;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AppointmentFailedEvent(Id, PatientId, DoctorId, reason));
    }

    private void EnsureStatus(params AppointmentStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
            throw new InvalidOperationException(
                $"Cannot transition from {Status}. Allowed: {string.Join(", ", allowedStatuses)}");
    }
}
