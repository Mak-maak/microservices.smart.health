using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Domain.Exceptions;
using SmartHealth.Appointments.Domain.ValueObjects;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Features.Appointments.BookAppointment;

// ---------------------------------------------------------------------------
// Command / Result
// ---------------------------------------------------------------------------

public sealed record BookAppointmentCommand(
    Guid PatientId,
    Guid DoctorId,
    DateTime StartTime,
    DateTime EndTime,
    string? Reason) : IRequest<BookAppointmentResult>;

public sealed record BookAppointmentResult(Guid AppointmentId);

// ---------------------------------------------------------------------------
// Validator
// ---------------------------------------------------------------------------

public sealed class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.StartTime).GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be scheduled in the future.");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
    }
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

public sealed class BookAppointmentHandler(AppointmentsDbContext db)
    : IRequestHandler<BookAppointmentCommand, BookAppointmentResult>
{
    public async Task<BookAppointmentResult> Handle(
        BookAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify patient and doctor exist
        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken))
            throw new PatientNotFoundException(request.PatientId);

        if (!await db.Doctors.AnyAsync(d => d.Id == request.DoctorId, cancellationToken))
            throw new DoctorNotFoundException(request.DoctorId);

        // Check for doctor double-booking
        bool doubleBooked = await db.Appointments.AnyAsync(
            a => a.DoctorId == request.DoctorId
                 && a.Slot.StartTime == request.StartTime
                 && a.Status != AppointmentStatus.Cancelled
                 && a.Status != AppointmentStatus.Failed,
            cancellationToken);

        if (doubleBooked)
            throw new DoctorDoubleBookingException(
                $"Doctor {request.DoctorId} already has an appointment at {request.StartTime}.");

        var slot = new AppointmentSlot(request.StartTime, request.EndTime);
        var appointment = Appointment.Book(
            request.PatientId, request.DoctorId, slot, request.Reason);

        db.Appointments.Add(appointment);

        // Domain event is automatically converted to outbox message by SaveChangesAsync override
        await db.SaveChangesAsync(cancellationToken);

        return new BookAppointmentResult(appointment.Id);
    }
}
