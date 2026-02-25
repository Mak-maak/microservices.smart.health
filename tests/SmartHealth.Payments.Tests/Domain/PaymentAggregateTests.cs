using SmartHealth.Payments.Domain.Entities;
using SmartHealth.Payments.Domain.Events;
using SmartHealth.Payments.Domain.Exceptions;

namespace SmartHealth.Payments.Tests.Domain;

public sealed class PaymentAggregateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsPendingPayment()
    {
        var appointmentId = Guid.NewGuid();
        var payment = Payment.Create(appointmentId, "user-123", 100m, "usd");

        payment.Id.Should().NotBeEmpty();
        payment.AppointmentId.Should().Be(appointmentId);
        payment.UserId.Should().Be("user-123");
        payment.Amount.Should().Be(100m);
        payment.Currency.Should().Be("usd");
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Create_WithZeroAmount_ThrowsInvalidPaymentAmountException()
    {
        var act = () => Payment.Create(Guid.NewGuid(), "user-123", 0m, "usd");
        act.Should().Throw<InvalidPaymentAmountException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsInvalidPaymentAmountException()
    {
        var act = () => Payment.Create(Guid.NewGuid(), "user-123", -50m, "usd");
        act.Should().Throw<InvalidPaymentAmountException>();
    }

    [Fact]
    public void Create_RaisesPaymentCreatedEvent()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentCreatedEvent);
    }

    [Fact]
    public void MarkProcessing_FromPending_TransitionsToProcessing()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.MarkProcessing("pi_test_123");

        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.StripePaymentIntentId.Should().Be("pi_test_123");
    }

    [Fact]
    public void MarkCompleted_FromProcessing_TransitionsToCompleted()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.MarkProcessing("pi_test_123");
        payment.MarkCompleted();

        payment.Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void MarkCompleted_RaisesPaymentCompletedEvent()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.MarkProcessing("pi_test_123");
        payment.MarkCompleted();

        payment.DomainEvents.Should().Contain(e => e is PaymentCompletedEvent);
    }

    [Fact]
    public void MarkFailed_FromPending_TransitionsToFailed()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.MarkFailed("Stripe error");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("Stripe error");
    }

    [Fact]
    public void MarkFailed_RaisesPaymentFailedEvent()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.MarkFailed("Stripe error");

        payment.DomainEvents.Should().Contain(e => e is PaymentFailedEvent);
    }

    [Fact]
    public void MarkCompleted_FromFailed_ThrowsInvalidOperationException()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.MarkFailed("error");

        var act = () => payment.MarkCompleted();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var payment = Payment.Create(Guid.NewGuid(), "user-123", 100m, "usd");
        payment.ClearDomainEvents();
        payment.DomainEvents.Should().BeEmpty();
    }
}
