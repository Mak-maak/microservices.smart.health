package com.smarthealth.audit.messaging;

public record AppointmentEventDto(
        String eventId,
        String eventType,
        String appointmentId,
        String patientId,
        String doctorId,
        String reason,
        String startTime,
        String endTime,
        String occurredAt
) {}
