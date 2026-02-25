using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace SmartHealth.Payments.Infrastructure.Stripe;

/// <summary>
/// Stripe payment integration using Stripe.net SDK in test mode.
///
/// Architectural note: This service wraps the Stripe API calls and
/// translates Stripe-specific exceptions into domain-friendly exceptions.
/// The IStripePaymentService abstraction allows swapping for a mock in tests.
/// </summary>
public sealed class StripePaymentService(
    IConfiguration configuration,
    ILogger<StripePaymentService> logger) : IStripePaymentService
{
    public async Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        // Configure Stripe with test API key
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Stripe uses smallest currency unit (cents)
            Currency = currency.ToLowerInvariant(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = new Dictionary<string, string>
            {
                { "appointmentId", appointmentId.ToString() }
            },
            Description = $"SmartHealth appointment payment for {appointmentId}"
        };

        var service = new PaymentIntentService();

        try
        {
            logger.LogInformation(
                "Creating Stripe PaymentIntent for appointment {AppointmentId}, amount {Amount} {Currency}",
                appointmentId, amount, currency);

            var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);

            logger.LogInformation(
                "Stripe PaymentIntent {IntentId} created for appointment {AppointmentId}",
                intent.Id, appointmentId);

            return intent.Id;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "Stripe error creating PaymentIntent for appointment {AppointmentId}: {Message}",
                appointmentId, ex.Message);
            throw;
        }
    }
}
