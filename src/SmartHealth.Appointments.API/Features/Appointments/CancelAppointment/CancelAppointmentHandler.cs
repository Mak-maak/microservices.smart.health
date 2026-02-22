using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Domain.Exceptions;
using SmartHealth.Appointments.Infrastructure.Outbox;
using SmartHealth.Appointments.Infrastructure.Persistence;
using System.Text.Json;
using SmartHealth.Appointments.Infrastructure.Messaging;

namespace SmartHealth.Appointments.Features.Appointments.CancelAppointment;

// ---------------------------------------------------------------------------
// Command / Result
// ---------------------------------------------------------------------------

public sealed record CancelAppointmentCommand(
    Guid AppointmentId,
    string Reason) : IRequest<CancelAppointmentResult>;

public sealed record CancelAppointmentResult(Guid AppointmentId, AppointmentStatus Status);

// ---------------------------------------------------------------------------
// Validator
// ---------------------------------------------------------------------------

public sealed class CancelAppointmentValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

public sealed class CancelAppointmentHandler(AppointmentsDbContext db)
    : IRequestHandler<CancelAppointmentCommand, CancelAppointmentResult>
{
    public async Task<CancelAppointmentResult> Handle(
        CancelAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new AppointmentNotFoundException(request.AppointmentId);

        appointment.Cancel(request.Reason);

        // Transactional Outbox
        db.OutboxMessages.Add(new OutboxMessage
        {
            MessageType = typeof(AppointmentCancelledIntegrationEvent).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(
                new AppointmentCancelledIntegrationEvent(appointment.Id, request.Reason)),
            CorrelationId = appointment.Id.ToString()
        });

        await db.SaveChangesAsync(cancellationToken);

        return new CancelAppointmentResult(appointment.Id, appointment.Status);
    }
}
