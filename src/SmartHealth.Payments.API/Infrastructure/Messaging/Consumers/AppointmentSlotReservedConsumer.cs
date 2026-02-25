using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartHealth.Payments.Features.Payments.CreatePayment;
using SmartHealth.Payments.Infrastructure.Messaging;

namespace SmartHealth.Payments.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes AppointmentSlotReservedEvent and initiates the payment flow.
///
/// Idempotency: The CreatePaymentHandler checks for an existing payment
/// with the same AppointmentId before creating a new one, ensuring
/// duplicate messages are safely ignored.
/// </summary>
public sealed class AppointmentSlotReservedConsumer(
    IMediator mediator,
    ILogger<AppointmentSlotReservedConsumer> logger)
    : IConsumer<AppointmentSlotReservedEvent>
{
    public async Task Consume(ConsumeContext<AppointmentSlotReservedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "Received AppointmentSlotReserved for appointment {AppointmentId}, user {UserId}",
            msg.AppointmentId, msg.UserId);

        var command = new CreatePaymentCommand(
            msg.AppointmentId,
            msg.UserId,
            msg.Amount,
            msg.Currency);

        await mediator.Send(command, context.CancellationToken);
    }
}
