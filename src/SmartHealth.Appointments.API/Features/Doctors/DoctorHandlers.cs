using FluentValidation;
using MediatR;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Features.Doctors;

// ---------------------------------------------------------------------------
// Create Doctor Command
// ---------------------------------------------------------------------------

public sealed record CreateDoctorCommand(
    string FirstName,
    string LastName,
    string Email,
    string Specialization,
    string LicenseNumber) : IRequest<Guid>;

public sealed class CreateDoctorValidator : AbstractValidator<CreateDoctorCommand>
{
    public CreateDoctorValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateDoctorHandler(AppointmentsDbContext db)
    : IRequestHandler<CreateDoctorCommand, Guid>
{
    public async Task<Guid> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = Doctor.Create(
            request.FirstName, request.LastName, request.Email,
            request.Specialization, request.LicenseNumber);

        db.Doctors.Add(doctor);
        await db.SaveChangesAsync(cancellationToken);
        return doctor.Id;
    }
}

// ---------------------------------------------------------------------------
// Get Doctor Query
// ---------------------------------------------------------------------------

public sealed record GetDoctorQuery(Guid DoctorId) : IRequest<DoctorDto?>;

public sealed record DoctorDto(
    Guid Id,
    string FullName,
    string Email,
    string Specialization,
    string LicenseNumber);

public sealed class GetDoctorHandler(AppointmentsDbContext db)
    : IRequestHandler<GetDoctorQuery, DoctorDto?>
{
    public async Task<DoctorDto?> Handle(GetDoctorQuery request, CancellationToken cancellationToken)
    {
        var doctor = await db.Doctors.FindAsync([request.DoctorId], cancellationToken);
        if (doctor is null) return null;
        return new DoctorDto(doctor.Id, doctor.FullName, doctor.Email,
            doctor.Specialization, doctor.LicenseNumber);
    }
}
