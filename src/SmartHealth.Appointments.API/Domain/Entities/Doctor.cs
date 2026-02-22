namespace SmartHealth.Appointments.Domain.Entities;

/// <summary>
/// Doctor aggregate root.
/// </summary>
public sealed class Doctor : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Specialization { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Doctor() { }

    public static Doctor Create(string firstName, string lastName, string email,
        string specialization, string licenseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);

        return new Doctor
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Specialization = specialization,
            LicenseNumber = licenseNumber
        };
    }

    public string FullName => $"Dr. {FirstName} {LastName}";
}
