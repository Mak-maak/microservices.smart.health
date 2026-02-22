using FluentAssertions;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Domain.Exceptions;
using SmartHealth.Appointments.Domain.ValueObjects;

namespace SmartHealth.Appointments.Tests.Domain;

/// <summary>Tests covering Appointment aggregate root business rules.</summary>
public sealed class AppointmentAggregateTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();

    [Fact]
    public void Book_WithFutureSlot_ShouldCreateAppointmentWithRequestedStatus()
    {
        var slot = FutureSlot();
        var appointment = Appointment.Book(PatientId, DoctorId, slot, "Regular check-up");

        appointment.Status.Should().Be(AppointmentStatus.Requested);
        appointment.PatientId.Should().Be(PatientId);
        appointment.DoctorId.Should().Be(DoctorId);
        appointment.Slot.Should().Be(slot);
        appointment.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Book_WithPastSlot_ShouldThrowAppointmentInPastException()
    {
        var pastSlot = new AppointmentSlot(
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1));

        var act = () => Appointment.Book(PatientId, DoctorId, pastSlot, null);

        act.Should().Throw<AppointmentInPastException>();
    }

    [Fact]
    public void Cancel_FromRequested_ShouldTransitionToCancelled()
    {
        var appointment = BookFutureAppointment();

        appointment.Cancel("Patient request");

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancellationReason.Should().Be("Patient request");
        appointment.DomainEvents.Should().HaveCount(2); // Requested + Cancelled
    }

    [Fact]
    public void Confirm_FromRequested_ShouldTransitionToConfirmed()
    {
        var appointment = BookFutureAppointment();
        appointment.ReserveSlot();

        appointment.Confirm();

        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        appointment.DomainEvents.Should().HaveCount(2); // Requested + Confirmed
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        var appointment = BookFutureAppointment();
        appointment.Cancel("first reason");

        var act = () => appointment.Cancel("second reason");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AppointmentSlot_EndBeforeStart_ShouldThrowArgumentException()
    {
        var now = DateTime.UtcNow;
        var act = () => new AppointmentSlot(now.AddHours(2), now.AddHours(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Patient_Create_WithValidData_ShouldSucceed()
    {
        var patient = Patient.Create("John", "Doe", "john@example.com", "555-1234",
            new DateOnly(1990, 1, 15));

        patient.FullName.Should().Be("John Doe");
        patient.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void Doctor_Create_WithEmptyLicenseNumber_ShouldThrow()
    {
        var act = () => Doctor.Create("Jane", "Smith", "jane@doc.com", "Cardiology", "");

        act.Should().Throw<ArgumentException>();
    }

    private static AppointmentSlot FutureSlot() =>
        new(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1));

    private static Appointment BookFutureAppointment() =>
        Appointment.Book(PatientId, DoctorId, FutureSlot(), "Check-up");
}
