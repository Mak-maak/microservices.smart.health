using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Infrastructure.Messaging;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.Saga.Consumers;

/// <summary>
/// Handles compensation (rollback) when appointment booking fails.
/// Cancels/fails the appointment and notifies the saga.
/// </summary>
public sealed class CompensateAppointmentConsumer : IConsumer<CompensateAppointmentCommand>
{
    private readonly AppointmentsDbContext _db;
    private readonly ILogger<CompensateAppointmentConsumer> _logger;

    public CompensateAppointmentConsumer(
        AppointmentsDbContext db,
        ILogger<CompensateAppointmentConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CompensateAppointmentCommand> context)
    {
        var message = context.Message;
        _logger.LogWarning(
            "Compensating appointment {AppointmentId}. Reason: {Reason}",
            message.AppointmentId, message.Reason);

        try
        {
            // Find the appointment
            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(
                    a => a.Id == message.AppointmentId,
                    context.CancellationToken);

            if (appointment == null)
            {
                _logger.LogWarning(
                    "Appointment {AppointmentId} not found during compensation",
                    message.AppointmentId);
                
                // Still publish compensation complete to allow saga to finish
                await context.Publish(new AppointmentCompensatedMessage(
                    message.AppointmentId,
                    "Appointment not found - compensation skipped"));
                return;
            }

            // Mark appointment as failed with the reason
            appointment.Fail(message.Reason);
            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Appointment {AppointmentId} marked as failed. Reason: {Reason}",
                message.AppointmentId, message.Reason);

            // Notify saga that compensation is complete
            await context.Publish(new AppointmentCompensatedMessage(
                message.AppointmentId,
                message.Reason));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error compensating appointment {AppointmentId}",
                message.AppointmentId);

            // Publish compensation complete even on error to prevent saga getting stuck
            await context.Publish(new AppointmentCompensatedMessage(
                message.AppointmentId,
                $"Compensation error: {ex.Message}"));
        }
    }
}
