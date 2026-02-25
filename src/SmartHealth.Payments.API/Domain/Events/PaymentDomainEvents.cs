namespace SmartHealth.Payments.Domain.Events;

/// <summary>Raised when a new payment record is created.</summary>
public sealed record PaymentCreatedEvent(
    Guid PaymentId,
    Guid AppointmentId,
    string UserId,
    decimal Amount,
    string Currency);

/// <summary>
/// Raised when payment is successfully completed.
/// This event is stored in the Outbox and published as PaymentCompletedIntegrationEvent.
/// </summary>
public sealed record PaymentCompletedEvent(
    Guid PaymentId,
    Guid AppointmentId,
    string TransactionId);

/// <summary>Raised when a payment fails.</summary>
public sealed record PaymentFailedEvent(
    Guid PaymentId,
    Guid AppointmentId,
    string Reason);
