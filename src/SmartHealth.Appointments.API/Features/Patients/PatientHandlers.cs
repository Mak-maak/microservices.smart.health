using FluentValidation;
using MediatR;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Features.Patients;

// ---------------------------------------------------------------------------
// Create Patient Command
// ---------------------------------------------------------------------------

public sealed record CreatePatientCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateOnly DateOfBirth) : IRequest<Guid>;

public sealed class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
    }
}

public sealed class CreatePatientHandler(AppointmentsDbContext db)
    : IRequestHandler<CreatePatientCommand, Guid>
{
    public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = Patient.Create(
            request.FirstName, request.LastName, request.Email,
            request.PhoneNumber, request.DateOfBirth);

        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);
        return patient.Id;
    }
}

// ---------------------------------------------------------------------------
// Get Patient Query
// ---------------------------------------------------------------------------

public sealed record GetPatientQuery(Guid PatientId) : IRequest<PatientDto?>;

public sealed record PatientDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    DateOnly DateOfBirth);

public sealed class GetPatientHandler(AppointmentsDbContext db)
    : IRequestHandler<GetPatientQuery, PatientDto?>
{
    public async Task<PatientDto?> Handle(GetPatientQuery request, CancellationToken cancellationToken)
    {
        var patient = await db.Patients.FindAsync([request.PatientId], cancellationToken);
        if (patient is null) return null;
        return new PatientDto(patient.Id, patient.FullName, patient.Email,
            patient.PhoneNumber, patient.DateOfBirth);
    }
}
