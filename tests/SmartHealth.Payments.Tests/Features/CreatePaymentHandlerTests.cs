using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHealth.Payments.Domain.Entities;
using SmartHealth.Payments.Features.Payments.CompletePayment;
using SmartHealth.Payments.Features.Payments.CreatePayment;
using SmartHealth.Payments.Infrastructure.Persistence;
using SmartHealth.Payments.Infrastructure.Stripe;

namespace SmartHealth.Payments.Tests.Features;

public sealed class CreatePaymentHandlerTests : IDisposable
{
    private readonly PaymentsDbContext _db;
    private readonly IMediator _mediator;

    public CreatePaymentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new PaymentsDbContext(options);
        _mediator = Substitute.For<IMediator>();

        // Default mediator behaviour: return a successful CompletePaymentResult
        _mediator
            .Send(Arg.Any<CompletePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci => new CompletePaymentResult(
                ci.Arg<CompletePaymentCommand>().PaymentId,
                "Completed",
                "pi_test_123"));
    }

    [Fact]
    public async Task Handle_NewPayment_CreatesPendingPayment()
    {
        var handler = new CreatePaymentHandler(_db, _mediator, NullLogger<CreatePaymentHandler>.Instance);
        var command = new CreatePaymentCommand(Guid.NewGuid(), "user-1", 150m, "usd");

        var result = await handler.Handle(command, CancellationToken.None);

        result.PaymentId.Should().NotBeEmpty();
        var payment = await _db.Payments.FindAsync(result.PaymentId);
        payment.Should().NotBeNull();
        payment!.AppointmentId.Should().Be(command.AppointmentId);
    }

    [Fact]
    public async Task Handle_DuplicateAppointment_ReturnsExistingPayment()
    {
        var appointmentId = Guid.NewGuid();
        var handler = new CreatePaymentHandler(_db, _mediator, NullLogger<CreatePaymentHandler>.Instance);
        var command = new CreatePaymentCommand(appointmentId, "user-1", 150m, "usd");

        // First call
        var result1 = await handler.Handle(command, CancellationToken.None);
        // Second call (duplicate)
        var result2 = await handler.Handle(command, CancellationToken.None);

        result1.PaymentId.Should().Be(result2.PaymentId);
        _db.Payments.Count().Should().Be(1);
    }

    [Fact]
    public async Task Handle_NewPayment_DispatchesCompletePaymentCommand()
    {
        var handler = new CreatePaymentHandler(_db, _mediator, NullLogger<CreatePaymentHandler>.Instance);
        var command = new CreatePaymentCommand(Guid.NewGuid(), "user-1", 150m, "usd");

        await handler.Handle(command, CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Any<CompletePaymentCommand>(),
            Arg.Any<CancellationToken>());
    }

    public void Dispose() => _db.Dispose();
}
