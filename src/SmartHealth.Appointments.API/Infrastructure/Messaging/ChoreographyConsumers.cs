using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Domain.Events;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.Messaging;

// ---------------------------------------------------------------------------
// Choreography-based alternative (no saga state machine)
//
// Each consumer reacts to an event independently; the aggregate is updated
// directly and the next event published.  Simpler for greenfield, but harder
// to monitor / recover than the orchestration approach.
// ---------------------------------------------------------------------------

/// <summary>
/// Handles AppointmentRequestedEvent in choreography mode.
/// Validates doctor availability and publishes the next event.
/// </summary>
public sealed class AppointmentRequestedConsumer(
    AppointmentsDbContext db,
    IPublishEndpoint publishEndpoint,
    ILogger<AppointmentRequestedConsumer> logger)
    : IConsumer<AppointmentRequestedEvent>
{
    public async Task Consume(ConsumeContext<AppointmentRequestedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Choreography: AppointmentRequested {Id}", msg.AppointmentId);

        // Check if doctor has a conflicting confirmed appointment in the slot
        bool doctorBusy = await db.Appointments.AnyAsync(
            a => a.DoctorId == msg.DoctorId
                 && a.Slot.StartTime == msg.StartTime
                 && a.Status != AppointmentStatus.Cancelled
                 && a.Status != AppointmentStatus.Failed
                 && a.Id != msg.AppointmentId,
            context.CancellationToken);

        if (doctorBusy)
        {
            await publishEndpoint.Publish(new DoctorUnavailableMessage(
                msg.AppointmentId, "Doctor already has an appointment at this time."),
                context.CancellationToken);
            return;
        }

        await publishEndpoint.Publish(
            new DoctorAvailabilityValidatedMessage(msg.AppointmentId, msg.DoctorId),
            context.CancellationToken);
    }
}

/// <summary>
/// Handles ConfirmAppointmentCommand in choreography mode.
/// Confirms the appointment and publishes AppointmentConfirmedIntegrationEvent.
/// </summary>
public sealed class ConfirmAppointmentConsumer(
    AppointmentsDbContext db,
    IPublishEndpoint publishEndpoint,
    ILogger<ConfirmAppointmentConsumer> logger)
    : IConsumer<ConfirmAppointmentCommand>
{
    public async Task Consume(ConsumeContext<ConfirmAppointmentCommand> context)
    {
        var appointmentId = context.Message.AppointmentId;
        var appointment = await db.Appointments.FindAsync(
            [appointmentId], context.CancellationToken);

        if (appointment is null)
        {
            logger.LogWarning("Appointment {Id} not found for confirmation.", appointmentId);
            return;
        }

        appointment.Confirm();
        await db.SaveChangesAsync(context.CancellationToken);

        await publishEndpoint.Publish(
            new AppointmentConfirmedIntegrationEvent(
                appointment.Id, appointment.PatientId, appointment.DoctorId),
            context.CancellationToken);

        logger.LogInformation("Appointment {Id} confirmed (choreography).", appointmentId);
    }
}

/// <summary>
/// Handles CompensateAppointmentCommand in choreography mode.
/// </summary>
public sealed class CompensateAppointmentConsumer(
    AppointmentsDbContext db,
    IPublishEndpoint publishEndpoint,
    ILogger<CompensateAppointmentConsumer> logger)
    : IConsumer<CompensateAppointmentCommand>
{
    public async Task Consume(ConsumeContext<CompensateAppointmentCommand> context)
    {
        var msg = context.Message;
        var appointment = await db.Appointments.FindAsync(
            [msg.AppointmentId], context.CancellationToken);

        if (appointment is null) return;

        appointment.Fail(msg.Reason);
        await db.SaveChangesAsync(context.CancellationToken);

        await publishEndpoint.Publish(
            new AppointmentCancelledIntegrationEvent(msg.AppointmentId, msg.Reason),
            context.CancellationToken);

        logger.LogInformation("Appointment {Id} failed/compensated.", msg.AppointmentId);
    }
}
