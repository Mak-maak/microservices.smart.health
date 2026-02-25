using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartHealth.Payments.Domain.Entities;
using SmartHealth.Payments.Domain.Exceptions;
using SmartHealth.Payments.Features.Payments.CompletePayment;
using SmartHealth.Payments.Infrastructure.Persistence;
using SmartHealth.Payments.Infrastructure.Stripe;

namespace SmartHealth.Payments.Features.Payments.CreatePayment;

// ---------------------------------------------------------------------------
// Command / Result
// ---------------------------------------------------------------------------

/// <summary>
/// Command to create a payment for a reserved appointment slot.
/// Triggered by consuming the AppointmentSlotReserved integration event.
/// </summary>
public sealed record CreatePaymentCommand(
    Guid AppointmentId,
    string UserId,
    decimal Amount,
    string Currency) : IRequest<CreatePaymentResult>;

public sealed record CreatePaymentResult(Guid PaymentId, string Status);

// ---------------------------------------------------------------------------
// Validator
// ---------------------------------------------------------------------------

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-letter ISO code.");
    }
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

/// <summary>
/// Handles the CreatePaymentCommand.
///
/// Flow:
///   1. Check idempotency – if payment already exists for this appointment, return existing.
///   2. Create Payment aggregate in Pending status.
///   3. Persist payment (within transaction).
///   4. Dispatch CompletePaymentCommand to process Stripe payment.
/// </summary>
public sealed class CreatePaymentHandler(
    PaymentsDbContext db,
    IMediator mediator,
    ILogger<CreatePaymentHandler> logger)
    : IRequestHandler<CreatePaymentCommand, CreatePaymentResult>
{
    public async Task<CreatePaymentResult> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        // Idempotency check: skip if payment already exists for this appointment
        var existing = await db.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == request.AppointmentId, cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation(
                "Payment for appointment {AppointmentId} already exists (id: {PaymentId}). Skipping.",
                request.AppointmentId, existing.Id);
            return new CreatePaymentResult(existing.Id, existing.Status.ToString());
        }

        // Create Payment aggregate
        var payment = Payment.Create(
            request.AppointmentId,
            request.UserId,
            request.Amount,
            request.Currency);

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} created for appointment {AppointmentId}.",
            payment.Id, request.AppointmentId);

        // Dispatch CompletePayment (process Stripe charge)
        await mediator.Send(
            new CompletePaymentCommand(payment.Id),
            cancellationToken);

        return new CreatePaymentResult(payment.Id, payment.Status.ToString());
    }
}
