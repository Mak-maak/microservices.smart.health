namespace SmartHealth.Appointments.Domain.Events;

/// <summary>Raised when an appointment is first requested.</summary>
public sealed record AppointmentRequestedEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime StartTime,
    DateTime EndTime);

/// <summary>Raised when an appointment is successfully confirmed.</summary>
public sealed record AppointmentConfirmedEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId);

/// <summary>Raised when an appointment is cancelled by the patient or system.</summary>
public sealed record AppointmentCancelledEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    string Reason);

/// <summary>Raised when the booking saga fails and compensates.</summary>
public sealed record AppointmentFailedEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    string Reason);
