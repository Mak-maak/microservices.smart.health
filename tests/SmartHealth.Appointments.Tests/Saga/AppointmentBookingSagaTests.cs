using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartHealth.Appointments.Infrastructure.Messaging;
using SmartHealth.Appointments.Infrastructure.Saga;

namespace SmartHealth.Appointments.Tests.Saga;

/// <summary>
/// Tests for the orchestration-based Appointment Booking Saga state machine.
/// Uses MassTransit's built-in InMemory test harness.
/// </summary>
public sealed class AppointmentBookingSagaTests : IAsyncLifetime
{
    private IServiceProvider _serviceProvider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _serviceProvider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<AppointmentBookingSaga, AppointmentSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider();

        _harness = _serviceProvider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await (_serviceProvider as IAsyncDisposable)?.DisposeAsync().AsTask()!;
    }

    [Fact]
    public async Task Saga_WhenDoctorAvailable_ShouldTransitionToConfirming()
    {
        var appointmentId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);

        // Publish the initial AppointmentRequested message
        await _harness.Bus.Publish(new AppointmentRequestedMessage(
            appointmentId,
            Guid.NewGuid(),
            doctorId,
            startTime,
            startTime.AddHours(1)));

        // Saga should publish ValidateDoctorAvailabilityCommand
        (await _harness.Published.Any<ValidateDoctorAvailabilityCommand>())
            .Should().BeTrue();

        // Simulate doctor available response
        await _harness.Bus.Publish(new DoctorAvailabilityValidatedMessage(appointmentId, doctorId));

        // Saga should then publish ReserveSlotCommand
        (await _harness.Published.Any<ReserveSlotCommand>())
            .Should().BeTrue();
    }

    [Fact]
    public async Task Saga_WhenDoctorUnavailable_ShouldPublishCompensation()
    {
        var appointmentId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(2);

        await _harness.Bus.Publish(new AppointmentRequestedMessage(
            appointmentId, Guid.NewGuid(), Guid.NewGuid(), startTime, startTime.AddHours(1)));

        // Simulate doctor unavailable
        await _harness.Bus.Publish(new DoctorUnavailableMessage(
            appointmentId, "Doctor fully booked"));

        // Saga should publish compensation command
        (await _harness.Published.Any<CompensateAppointmentCommand>())
            .Should().BeTrue();
    }

    [Fact]
    public async Task Saga_HappyPath_ShouldPublishConfirmedIntegrationEvent()
    {
        var appointmentId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(3);
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        await _harness.Bus.Publish(new AppointmentRequestedMessage(
            appointmentId, patientId, doctorId, startTime, startTime.AddHours(1)));

        await _harness.Bus.Publish(new DoctorAvailabilityValidatedMessage(appointmentId, doctorId));
        await _harness.Bus.Publish(new SlotReservedMessage(appointmentId, doctorId));
        await _harness.Bus.Publish(new AppointmentConfirmedMessage(appointmentId));

        // Final integration event should be published
        (await _harness.Published.Any<AppointmentConfirmedIntegrationEvent>())
            .Should().BeTrue();
    }
}
