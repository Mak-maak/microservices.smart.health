using SmartHealth.Payments.Domain.Events;
using SmartHealth.Payments.Domain.Exceptions;

namespace SmartHealth.Payments.Domain.Entities;

/// <summary>
/// Payment aggregate root.
/// Business rules:
///   – A payment is created for an appointment slot reservation.
///   – A payment may only transition: Pending → Processing → Completed | Failed.
///   – Idempotency: duplicate AppointmentSlotReserved events are ignored
///     (checked at handler level by AppointmentId uniqueness).
/// </summary>
public sealed class Payment : BaseEntity
{
    public Guid AppointmentId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    // Required by EF Core
    private Payment() { }

    /// <summary>Factory – creates a new payment in Pending status.</summary>
    public static Payment Create(Guid appointmentId, string userId, decimal amount, string currency)
    {
        if (amount <= 0)
            throw new InvalidPaymentAmountException("Payment amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified.", nameof(currency));

        var payment = new Payment
        {
            AppointmentId = appointmentId,
            UserId = userId,
            Amount = amount,
            Currency = currency.ToLowerInvariant(),
            Status = PaymentStatus.Pending
        };

        payment.AddDomainEvent(new PaymentCreatedEvent(payment.Id, appointmentId, userId, amount, currency));
        return payment;
    }

    /// <summary>Marks payment as Processing (Stripe intent created).</summary>
    public void MarkProcessing(string stripePaymentIntentId)
    {
        EnsureStatus(PaymentStatus.Pending);
        StripePaymentIntentId = stripePaymentIntentId;
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Marks payment as Completed and raises PaymentCompletedEvent for outbox.</summary>
    public void MarkCompleted()
    {
        EnsureStatus(PaymentStatus.Processing, PaymentStatus.Pending);
        Status = PaymentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentCompletedEvent(
            Id, AppointmentId, StripePaymentIntentId ?? string.Empty));
    }

    /// <summary>Marks payment as Failed.</summary>
    public void MarkFailed(string reason)
    {
        EnsureStatus(PaymentStatus.Processing, PaymentStatus.Pending);
        FailureReason = reason;
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentFailedEvent(Id, AppointmentId, reason));
    }

    private void EnsureStatus(params PaymentStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
            throw new InvalidOperationException(
                $"Cannot transition from {Status}. Allowed: {string.Join(", ", allowedStatuses)}");
    }
}
