namespace SmartHealth.Appointments.Domain.Entities;

/// <summary>Status lifecycle for an appointment.</summary>
public enum AppointmentStatus
{
    Requested = 0,
    SlotReserved = 1,
    Confirmed = 2,
    Cancelled = 3,
    Failed = 4
}
