package com.smarthealth.audit.messaging;

public record PaymentEventDto(
        String eventId,
        String eventType,
        String paymentId,
        String appointmentId,
        String status,
        String transactionId,
        String reason,
        String occurredAt
) {}
