namespace SmartHealth.Payments.Infrastructure.Messaging;

// ---------------------------------------------------------------------------
// Incoming integration events (consumed by this service)
// ---------------------------------------------------------------------------

/// <summary>
/// Published by the Appointments service when a slot is reserved.
/// This service consumes this event to initiate payment processing.
/// </summary>
public sealed record AppointmentSlotReservedEvent(
    Guid AppointmentId,
    string UserId,
    decimal Amount,
    string Currency);

// ---------------------------------------------------------------------------
// Outgoing integration events (published by this service via Outbox)
// ---------------------------------------------------------------------------

/// <summary>
/// Published by this service when a payment is successfully completed.
/// Consumed by the Appointments service to confirm the appointment.
/// </summary>
public sealed record PaymentCompletedIntegrationEvent(
    Guid PaymentId,
    Guid AppointmentId,
    string Status,
    string TransactionId);

/// <summary>Published when a payment fails, enabling compensating transactions.</summary>
public sealed record PaymentFailedIntegrationEvent(
    Guid PaymentId,
    Guid AppointmentId,
    string Reason);
