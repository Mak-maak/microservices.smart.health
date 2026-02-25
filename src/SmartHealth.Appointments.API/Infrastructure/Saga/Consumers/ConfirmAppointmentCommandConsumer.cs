using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Infrastructure.Messaging;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.Saga.Consumers;

/// <summary>
/// Confirms the appointment after all validations and reservations succeed.
/// Responds with AppointmentConfirmedMessage.
/// </summary>
public sealed class ConfirmAppointmentCommandConsumer : IConsumer<ConfirmAppointmentCommand>
{
    private readonly AppointmentsDbContext _db;
    private readonly ILogger<ConfirmAppointmentCommandConsumer> _logger;

    public ConfirmAppointmentCommandConsumer(
        AppointmentsDbContext db,
        ILogger<ConfirmAppointmentCommandConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConfirmAppointmentCommand> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Confirming appointment {AppointmentId}",
            message.AppointmentId);

        try
        {
            // Find the appointment
            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(
                    a => a.Id == message.AppointmentId,
                    context.CancellationToken);

            if (appointment == null)
            {
                _logger.LogError("Appointment {AppointmentId} not found", message.AppointmentId);
                return; // Saga will timeout
            }

            // Confirm the appointment
            appointment.Confirm();
            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Appointment {AppointmentId} confirmed successfully",
                message.AppointmentId);

            await context.Publish(new AppointmentConfirmedMessage(
                message.AppointmentId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error confirming appointment {AppointmentId}",
                message.AppointmentId);
            // Note: In a real system, you might want to publish a failure event here
        }
    }
}
