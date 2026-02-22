using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Domain.Exceptions;
using SmartHealth.Appointments.Domain.ValueObjects;
using SmartHealth.Appointments.Features.Appointments.BookAppointment;
using SmartHealth.Appointments.Features.Appointments.CancelAppointment;
using SmartHealth.Appointments.Features.Appointments.GetAppointment;
using SmartHealth.Appointments.Infrastructure.Caching;
using SmartHealth.Appointments.Infrastructure.Persistence;

namespace SmartHealth.Appointments.Tests.Features;

/// <summary>Feature-level unit tests using EF Core InMemory provider.</summary>
public sealed class AppointmentFeatureTests : IDisposable
{
    private readonly AppointmentsDbContext _db;
    private readonly RedisCacheService _cache;

    public AppointmentFeatureTests()
    {
        var options = new DbContextOptionsBuilder<AppointmentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppointmentsDbContext(options);
        _cache = new RedisCacheService(new NoOpDistributedCache(), NullLogger<RedisCacheService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task BookAppointment_WithValidRequest_ShouldCreateAppointment()
    {
        var (patientId, doctorId) = await SeedPatientAndDoctor();

        var handler = new BookAppointmentHandler(_db);
        var command = new BookAppointmentCommand(
            patientId, doctorId,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1),
            "Check-up");

        var result = await handler.Handle(command, CancellationToken.None);

        result.AppointmentId.Should().NotBeEmpty();
        var appointment = await _db.Appointments.FindAsync(result.AppointmentId);
        appointment.Should().NotBeNull();
        appointment!.Status.Should().Be(AppointmentStatus.Requested);
        // Outbox message should be saved
        _db.OutboxMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task BookAppointment_WithNonExistentPatient_ShouldThrowPatientNotFoundException()
    {
        var handler = new BookAppointmentHandler(_db);
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1),
            null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PatientNotFoundException>();
    }

    [Fact]
    public async Task BookAppointment_WithDoubleBookedDoctor_ShouldThrowDoctorDoubleBookingException()
    {
        var (patientId, doctorId) = await SeedPatientAndDoctor();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(1);

        // First booking
        var handler = new BookAppointmentHandler(_db);
        await handler.Handle(new BookAppointmentCommand(patientId, doctorId, startTime, endTime, null),
            CancellationToken.None);

        // Second booking for same slot
        var act = async () => await handler.Handle(
            new BookAppointmentCommand(patientId, doctorId, startTime, endTime, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DoctorDoubleBookingException>();
    }

    [Fact]
    public async Task CancelAppointment_WithValidId_ShouldCancelAppointment()
    {
        var (patientId, doctorId) = await SeedPatientAndDoctor();
        var slot = new AppointmentSlot(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1));
        var appointment = Appointment.Book(patientId, doctorId, slot, null);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var handler = new CancelAppointmentHandler(_db);
        var result = await handler.Handle(
            new CancelAppointmentCommand(appointment.Id, "Conflict"),
            CancellationToken.None);

        result.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAppointment_WithNonExistentId_ShouldThrowNotFoundException()
    {
        var handler = new CancelAppointmentHandler(_db);
        var act = async () => await handler.Handle(
            new CancelAppointmentCommand(Guid.NewGuid(), "Reason"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AppointmentNotFoundException>();
    }

    [Fact]
    public async Task GetAppointment_WithValidId_ShouldReturnDto()
    {
        var (patientId, doctorId) = await SeedPatientAndDoctor();
        var slot = new AppointmentSlot(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(1));
        var appointment = Appointment.Book(patientId, doctorId, slot, "Annual check-up");
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var handler = new GetAppointmentHandler(_db, _cache);
        var dto = await handler.Handle(new GetAppointmentQuery(appointment.Id), CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(appointment.Id);
        dto.Reason.Should().Be("Annual check-up");
    }

    [Fact]
    public async Task GetAppointment_WithNonExistentId_ShouldThrowNotFoundException()
    {
        var handler = new GetAppointmentHandler(_db, _cache);
        var act = async () => await handler.Handle(
            new GetAppointmentQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AppointmentNotFoundException>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<(Guid PatientId, Guid DoctorId)> SeedPatientAndDoctor()
    {
        var patient = Patient.Create("Alice", "Smith", $"alice{Guid.NewGuid()}@example.com", "", new DateOnly(1985, 5, 10));
        var doctor = Doctor.Create("Bob", "Jones", $"bob{Guid.NewGuid()}@hospital.com", "GP", $"LIC{Guid.NewGuid():N}");
        _db.Patients.Add(patient);
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return (patient.Id, doctor.Id);
    }

    // Minimal no-op IDistributedCache for tests
    private sealed class NoOpDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => null;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;
    }
}
