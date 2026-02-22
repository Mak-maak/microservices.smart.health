namespace SmartHealth.Appointments.Domain.Entities;

/// <summary>
/// Patient aggregate root.
/// </summary>
public sealed class Patient : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // EF Core requires a parameterless constructor
    private Patient() { }

    public static Patient Create(string firstName, string lastName, string email,
        string phoneNumber, DateOnly dateOfBirth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth
        };

        return patient;
    }

    public string FullName => $"{FirstName} {LastName}";
}
