namespace SmartHealth.Appointments.Infrastructure.Messaging;

// ---------------------------------------------------------------------------
// Integration events (published to Azure Service Bus)
// ---------------------------------------------------------------------------

/// <summary>Published when a patient requests a new appointment.</summary>
public sealed record AppointmentRequestedMessage(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime StartTime,
    DateTime EndTime);

/// <summary>Published by the Doctor service when the doctor is available.</summary>
public sealed record DoctorAvailabilityValidatedMessage(Guid AppointmentId, Guid DoctorId);

/// <summary>Published by the Doctor service when the doctor is NOT available.</summary>
public sealed record DoctorUnavailableMessage(Guid AppointmentId, string Reason);

/// <summary>Published when a time slot has been reserved.</summary>
public sealed record SlotReservedMessage(Guid AppointmentId, Guid DoctorId);

/// <summary>Published when a time slot reservation fails.</summary>
public sealed record SlotReservationFailedMessage(Guid AppointmentId, string Reason);

/// <summary>Published when the appointment has been confirmed.</summary>
public sealed record AppointmentConfirmedMessage(Guid AppointmentId);

/// <summary>External integration event published on successful confirmation.</summary>
public sealed record AppointmentConfirmedIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId);

/// <summary>External integration event published when an appointment is cancelled.</summary>
public sealed record AppointmentCancelledIntegrationEvent(
    Guid AppointmentId,
    string Reason);

// ---------------------------------------------------------------------------
// Commands (choreography helpers sent between services)
// ---------------------------------------------------------------------------

public sealed record ValidateDoctorAvailabilityCommand(
    Guid AppointmentId,
    Guid DoctorId,
    DateTime StartTime,
    DateTime EndTime);

public sealed record ReserveSlotCommand(
    Guid AppointmentId,
    Guid DoctorId,
    DateTime StartTime,
    DateTime EndTime);

public sealed record ConfirmAppointmentCommand(Guid AppointmentId);

public sealed record CompensateAppointmentCommand(Guid AppointmentId, string Reason);

/// <summary>Published when compensation (rollback) is complete.</summary>
public sealed record AppointmentCompensatedMessage(Guid AppointmentId, string Reason);

