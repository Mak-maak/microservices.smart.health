namespace SmartHealth.Appointments.Domain.Exceptions;

public sealed class AppointmentInPastException(string message) : Exception(message);

public sealed class DoctorDoubleBookingException(string message) : Exception(message);

public sealed class AppointmentNotFoundException(Guid appointmentId)
    : Exception($"Appointment {appointmentId} was not found.");

public sealed class PatientNotFoundException(Guid patientId)
    : Exception($"Patient {patientId} was not found.");

public sealed class DoctorNotFoundException(Guid doctorId)
    : Exception($"Doctor {doctorId} was not found.");
