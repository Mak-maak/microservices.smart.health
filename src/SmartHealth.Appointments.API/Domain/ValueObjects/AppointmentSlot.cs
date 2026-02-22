namespace SmartHealth.Appointments.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a time slot.
/// </summary>
public sealed record AppointmentSlot
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }

    public AppointmentSlot(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time.");

        StartTime = startTime;
        EndTime = endTime;
    }

    public TimeSpan Duration => EndTime - StartTime;
}
