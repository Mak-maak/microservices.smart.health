using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHealth.Appointments.Domain.Entities;

namespace SmartHealth.Appointments.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(p => p.Email).IsUnique();
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

internal sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.LastName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(d => d.Email).IsUnique();
        builder.Property(d => d.Specialization).HasMaxLength(200);
        builder.Property(d => d.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(d => d.LicenseNumber).IsUnique();
        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);

        // Owned value object: AppointmentSlot
        builder.OwnsOne(a => a.Slot, slot =>
        {
            slot.Property(s => s.StartTime).HasColumnName("SlotStartTime").IsRequired();
            slot.Property(s => s.EndTime).HasColumnName("SlotEndTime").IsRequired();
        });

        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);
        builder.Property(a => a.RowVersion).IsRowVersion();

        // Note: Double-booking prevention is enforced in code (BookAppointmentHandler).
        // A database-level unique index on the SQL column is in the EF migration for SQL Server.
        // We skip the index definition here to remain compatible with InMemory provider (tests).

        builder.HasOne(a => a.Patient).WithMany()
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor).WithMany()
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}
