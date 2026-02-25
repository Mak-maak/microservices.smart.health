using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Infrastructure.Messaging;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Infrastructure.Saga.Consumers;

/// <summary>
/// Validates doctor availability for the requested appointment time slot.
/// Responds with DoctorAvailabilityValidatedMessage or DoctorUnavailableMessage.
/// </summary>
public sealed class ValidateDoctorAvailabilityConsumer : IConsumer<ValidateDoctorAvailabilityCommand>
{
    private readonly AppointmentsDbContext _db;
    private readonly ILogger<ValidateDoctorAvailabilityConsumer> _logger;

    public ValidateDoctorAvailabilityConsumer(
        AppointmentsDbContext db,
        ILogger<ValidateDoctorAvailabilityConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ValidateDoctorAvailabilityCommand> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Validating doctor {DoctorId} availability for appointment {AppointmentId}",
            message.DoctorId, message.AppointmentId);

        try
        {
            // Check if doctor exists
            var doctorExists = await _db.Doctors
                .AnyAsync(d => d.Id == message.DoctorId, context.CancellationToken);

            if (!doctorExists)
            {
                _logger.LogWarning("Doctor {DoctorId} not found", message.DoctorId);
                await context.Publish(new DoctorUnavailableMessage(
                    message.AppointmentId,
                    "Doctor not found"));
                return;
            }

            // Check for conflicting appointments
            var hasConflict = await _db.Appointments
                .AnyAsync(a =>
                    a.DoctorId == message.DoctorId &&
                    a.Status == AppointmentStatus.SlotReserved &&
                    a.Status == AppointmentStatus.Confirmed &&
                    // Time slot overlap check
                    ((a.Slot.StartTime < message.EndTime && a.Slot.EndTime > message.StartTime)),
                    context.CancellationToken);

            if (hasConflict)
            {
                _logger.LogWarning(
                    "Doctor {DoctorId} has conflicting appointment for slot {StartTime}-{EndTime}",
                    message.DoctorId, message.StartTime, message.EndTime);

                await context.Publish(new DoctorUnavailableMessage(
                    message.AppointmentId,
                    "Doctor has a conflicting appointment in this time slot"));
            }
            else
            {
                _logger.LogInformation(
                    "Doctor {DoctorId} is available for appointment {AppointmentId}",
                    message.DoctorId, message.AppointmentId);

                await context.Publish(new DoctorAvailabilityValidatedMessage(
                    message.AppointmentId,
                    message.DoctorId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating doctor availability for appointment {AppointmentId}",
                message.AppointmentId);

            // Publish unavailable on error to fail the saga gracefully
            await context.Publish(new DoctorUnavailableMessage(
                message.AppointmentId,
                $"Validation error: {ex.Message}"));
        }
    }
}
