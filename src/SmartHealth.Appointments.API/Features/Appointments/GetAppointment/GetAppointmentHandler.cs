using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Domain.Exceptions;
using SmartHealth.Appointments.Infrastructure.Caching;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Features.Appointments.GetAppointment;

// ---------------------------------------------------------------------------
// Query / Result
// ---------------------------------------------------------------------------

public sealed record GetAppointmentQuery(Guid AppointmentId) : IRequest<AppointmentDto>;

public sealed record AppointmentDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    string PatientName,
    string DoctorName,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    string? Reason);

// ---------------------------------------------------------------------------
// Handler
// ---------------------------------------------------------------------------

public sealed class GetAppointmentHandler(
    AppointmentsDbContext db,
    RedisCacheService cache)
    : IRequestHandler<GetAppointmentQuery, AppointmentDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public async Task<AppointmentDto> Handle(
        GetAppointmentQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"appointment:{request.AppointmentId}";

        var result = await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var appointment = await db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
                ?? throw new AppointmentNotFoundException(request.AppointmentId);

            return new AppointmentDto(
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.Patient!.FullName,
                appointment.Doctor!.FullName,
                appointment.Slot.StartTime,
                appointment.Slot.EndTime,
                appointment.Status,
                appointment.Reason);
        }, CacheTtl, cancellationToken);

        return result ?? throw new AppointmentNotFoundException(request.AppointmentId);
    }
}
