namespace SmartHealth.Payments.Infrastructure.Stripe;

/// <summary>Abstraction over the Stripe payment API for testability.</summary>
public interface IStripePaymentService
{
    /// <summary>
    /// Creates a Stripe PaymentIntent in test mode.
    /// Returns the PaymentIntent ID on success, throws on failure.
    /// </summary>
    Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
