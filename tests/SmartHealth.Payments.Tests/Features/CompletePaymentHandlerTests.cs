using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHealth.Payments.Domain.Entities;
using SmartHealth.Payments.Domain.Exceptions;
using SmartHealth.Payments.Features.Payments.CompletePayment;
using SmartHealth.Payments.Infrastructure.Persistence;
using SmartHealth.Payments.Infrastructure.Stripe;

namespace SmartHealth.Payments.Tests.Features;

public sealed class CompletePaymentHandlerTests : IDisposable
{
    private readonly PaymentsDbContext _db;
    private readonly IStripePaymentService _stripeService;

    public CompletePaymentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new PaymentsDbContext(options);
        _stripeService = Substitute.For<IStripePaymentService>();
    }

    private Payment CreateAndSavePayment(Guid? appointmentId = null)
    {
        var payment = Payment.Create(
            appointmentId ?? Guid.NewGuid(), "user-1", 100m, "usd");
        _db.Payments.Add(payment);
        _db.SaveChanges();
        // Clear domain events after save to avoid duplication in test assertions
        payment.ClearDomainEvents();
        return payment;
    }

    [Fact]
    public async Task Handle_StripeSuccess_MarksPaymentCompleted()
    {
        var payment = CreateAndSavePayment();
        _stripeService
            .CreatePaymentIntentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("pi_test_success");

        var handler = new CompletePaymentHandler(_db, _stripeService, NullLogger<CompletePaymentHandler>.Instance);
        var result = await handler.Handle(new CompletePaymentCommand(payment.Id), CancellationToken.None);

        result.Status.Should().Be("Completed");
        result.TransactionId.Should().Be("pi_test_success");
    }

    [Fact]
    public async Task Handle_StripeFailure_MarksPaymentFailed()
    {
        var payment = CreateAndSavePayment();
        _stripeService
            .CreatePaymentIntentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new Stripe.StripeException("Card declined"));

        var handler = new CompletePaymentHandler(_db, _stripeService, NullLogger<CompletePaymentHandler>.Instance);
        var result = await handler.Handle(new CompletePaymentCommand(payment.Id), CancellationToken.None);

        result.Status.Should().Be("Failed");
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsPaymentNotFoundException()
    {
        var handler = new CompletePaymentHandler(_db, _stripeService, NullLogger<CompletePaymentHandler>.Instance);
        var act = async () => await handler.Handle(new CompletePaymentCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task Handle_StripeSuccess_WritesOutboxMessage()
    {
        var payment = CreateAndSavePayment();
        _stripeService
            .CreatePaymentIntentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("pi_test_outbox");

        var handler = new CompletePaymentHandler(_db, _stripeService, NullLogger<CompletePaymentHandler>.Instance);
        await handler.Handle(new CompletePaymentCommand(payment.Id), CancellationToken.None);

        var outboxMessages = await _db.OutboxMessages.ToListAsync();
        outboxMessages.Should().ContainSingle();
        outboxMessages[0].MessageType.Should().Contain("PaymentCompletedIntegrationEvent");
    }

    public void Dispose() => _db.Dispose();
}
