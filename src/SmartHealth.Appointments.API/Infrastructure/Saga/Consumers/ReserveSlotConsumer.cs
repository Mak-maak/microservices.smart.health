using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Infrastructure.Messaging;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.Saga.Consumers;

/// <summary>
/// Reserves the appointment slot for the doctor.
/// Responds with SlotReservedMessage or SlotReservationFailedMessage.
/// </summary>
public sealed class ReserveSlotConsumer : IConsumer<ReserveSlotCommand>
{
    private readonly AppointmentsDbContext _db;
    private readonly ILogger<ReserveSlotConsumer> _logger;

    public ReserveSlotConsumer(
        AppointmentsDbContext db,
        ILogger<ReserveSlotConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveSlotCommand> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Reserving slot for appointment {AppointmentId}",
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
                await context.Publish(new SlotReservationFailedMessage(
                    message.AppointmentId,
                    "Appointment not found"));
                return;
            }

            // Reserve the slot (update status)
            appointment.ReserveSlot();
            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Slot reserved successfully for appointment {AppointmentId}",
                message.AppointmentId);

            await context.Publish(new SlotReservedMessage(
                message.AppointmentId,
                message.DoctorId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error reserving slot for appointment {AppointmentId}",
                message.AppointmentId);

            await context.Publish(new SlotReservationFailedMessage(
                message.AppointmentId,
                $"Reservation error: {ex.Message}"));
        }
    }
}
