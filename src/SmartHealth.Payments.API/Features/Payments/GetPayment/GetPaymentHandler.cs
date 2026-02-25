using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Payments.Domain.Exceptions;
using SmartHealth.Payments.Infrastructure.Persistence;

namespace SmartHealth.Payments.Features.Payments.GetPayment;

// ---------------------------------------------------------------------------
// Query / Result
// ---------------------------------------------------------------------------

public sealed record GetPaymentQuery(Guid PaymentId) : IRequest<GetPaymentResult>;

public sealed record GetPaymentResult(
    Guid PaymentId,
    Guid AppointmentId,
    string UserId,
    decimal Amount,
    string Currency,
    string Status,
    string? StripePaymentIntentId,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

public sealed class GetPaymentHandler(PaymentsDbContext db)
    : IRequestHandler<GetPaymentQuery, GetPaymentResult>
{
    public async Task<GetPaymentResult> Handle(
        GetPaymentQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        return new GetPaymentResult(
            payment.Id,
            payment.AppointmentId,
            payment.UserId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.StripePaymentIntentId,
            payment.FailureReason,
            payment.CreatedAt,
            payment.UpdatedAt);
    }
}
