using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartHealth.Payments.Domain.Exceptions;
using SmartHealth.Payments.Infrastructure.Persistence;
using SmartHealth.Payments.Infrastructure.Stripe;
using Stripe;

namespace SmartHealth.Payments.Features.Payments.CompletePayment;

// ---------------------------------------------------------------------------
// Command / Result
// ---------------------------------------------------------------------------

/// <summary>
/// Command to process a payment via Stripe and mark it Completed or Failed.
/// </summary>
public sealed record CompletePaymentCommand(Guid PaymentId) : IRequest<CompletePaymentResult>;

public sealed record CompletePaymentResult(Guid PaymentId, string Status, string? TransactionId);

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

/// <summary>
/// Handles the CompletePaymentCommand.
///
/// Flow:
///   1. Load Payment aggregate.
///   2. Call Stripe to create PaymentIntent.
///   3. On success: mark Completed, persist (outbox populated automatically).
///   4. On Stripe failure: mark Failed, persist (outbox populated automatically).
///
/// The PaymentCompleted domain event is translated to PaymentCompletedIntegrationEvent
/// in the Outbox by PaymentsDbContext.SaveChangesAsync.
/// </summary>
public sealed class CompletePaymentHandler(
    PaymentsDbContext db,
    IStripePaymentService stripeService,
    ILogger<CompletePaymentHandler> logger)
    : IRequestHandler<CompletePaymentCommand, CompletePaymentResult>
{
    public async Task<CompletePaymentResult> Handle(
        CompletePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FindAsync([request.PaymentId], cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        try
        {
            logger.LogInformation(
                "Processing Stripe payment for payment {PaymentId}, appointment {AppointmentId}",
                payment.Id, payment.AppointmentId);

            // Call Stripe to create payment intent
            var intentId = await stripeService.CreatePaymentIntentAsync(
                payment.Amount,
                payment.Currency,
                payment.AppointmentId,
                cancellationToken);

            // Mark as processing (intent created)
            payment.MarkProcessing(intentId);

            // Mark as completed (in test mode, we auto-confirm)
            payment.MarkCompleted();

            logger.LogInformation(
                "Payment {PaymentId} completed successfully. TransactionId: {TransactionId}",
                payment.Id, intentId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "Stripe error processing payment {PaymentId}: {Message}",
                payment.Id, ex.Message);

            payment.MarkFailed($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unexpected error processing payment {PaymentId}: {Message}",
                payment.Id, ex.Message);

            payment.MarkFailed($"Payment processing error: {ex.Message}");
        }

        // Persist changes – outbox messages are auto-populated by DbContext
        await db.SaveChangesAsync(cancellationToken);

        return new CompletePaymentResult(
            payment.Id,
            payment.Status.ToString(),
            payment.StripePaymentIntentId);
    }
}
