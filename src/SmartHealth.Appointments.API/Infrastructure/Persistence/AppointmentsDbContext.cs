using Microsoft.EntityFrameworkCore;
using SmartHealth.Appointments.Domain.Entities;
using SmartHealth.Appointments.Infrastructure.EventSourcing;
using SmartHealth.Appointments.Infrastructure.Outbox;

namespace SmartHealth.Appointments.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext for the Appointments microservice.
/// Supports:
///   – Domain entities (Patients, Doctors, Appointments)
///   – Outbox messages (transactional outbox pattern)
///   – Event store (optional event sourcing)
///   – MassTransit saga state
/// </summary>
public sealed class AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options)
    : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<EventStoreEntry> EventStoreEntries => Set<EventStoreEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppointmentsDbContext).Assembly);
    }
}
