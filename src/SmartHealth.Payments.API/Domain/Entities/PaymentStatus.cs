namespace SmartHealth.Payments.Domain.Entities;

/// <summary>Represents the lifecycle states of a payment.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
