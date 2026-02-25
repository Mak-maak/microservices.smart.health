namespace SmartHealth.Payments.Domain.Exceptions;

/// <summary>Thrown when a payment record is not found.</summary>
public sealed class PaymentNotFoundException(Guid paymentId)
    : Exception($"Payment {paymentId} was not found.");

/// <summary>Thrown when attempting to process a duplicate payment for the same appointment.</summary>
public sealed class DuplicatePaymentException(Guid appointmentId)
    : Exception($"A payment for appointment {appointmentId} already exists.");

/// <summary>Thrown when payment amount is invalid.</summary>
public sealed class InvalidPaymentAmountException(string message) : Exception(message);
