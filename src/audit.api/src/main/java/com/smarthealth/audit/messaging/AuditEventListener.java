package com.smarthealth.audit.messaging;

import com.azure.messaging.servicebus.ServiceBusErrorContext;
import com.azure.messaging.servicebus.ServiceBusReceivedMessageContext;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.smarthealth.audit.domain.ActorType;
import com.smarthealth.audit.features.storeaudit.StoreAuditEntryCommand;
import com.smarthealth.audit.features.storeaudit.StoreAuditEntryHandler;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.time.Instant;
import java.util.UUID;

@Slf4j
@Component
@RequiredArgsConstructor
public class AuditEventListener {

    private final StoreAuditEntryHandler storeAuditEntryHandler;
    private final ObjectMapper objectMapper;

    public void processAppointmentMessage(ServiceBusReceivedMessageContext context) {
        try {
            String body = context.getMessage().getBody().toString();
            AppointmentEventDto dto = objectMapper.readValue(body, AppointmentEventDto.class);
            log.info("Received appointment event: type={} appointmentId={}", dto.eventType(), dto.appointmentId());

            StoreAuditEntryCommand command = new StoreAuditEntryCommand(
                    parseUuid(dto.eventId()),
                    dto.eventType(),
                    "Appointment",
                    dto.appointmentId(),
                    null,
                    "appointments-api",
                    null,
                    ActorType.SYSTEM,
                    null,
                    buildAppointmentPayload(dto),
                    null,
                    parseInstant(dto.occurredAt()),
                    0L
            );

            storeAuditEntryHandler.handle(command);
            context.complete();
        } catch (Exception e) {
            log.error("Failed to process appointment message, abandoning: {}", e.getMessage(), e);
            context.abandon();
        }
    }

    public void processPaymentMessage(ServiceBusReceivedMessageContext context) {
        try {
            String body = context.getMessage().getBody().toString();
            PaymentEventDto dto = objectMapper.readValue(body, PaymentEventDto.class);
            log.info("Received payment event: type={} paymentId={}", dto.eventType(), dto.paymentId());

            StoreAuditEntryCommand command = new StoreAuditEntryCommand(
                    parseUuid(dto.eventId()),
                    dto.eventType(),
                    "Payment",
                    dto.paymentId(),
                    dto.appointmentId(),
                    "payments-api",
                    null,
                    ActorType.SYSTEM,
                    null,
                    buildPaymentPayload(dto),
                    null,
                    parseInstant(dto.occurredAt()),
                    0L
            );

            storeAuditEntryHandler.handle(command);
            context.complete();
        } catch (Exception e) {
            log.error("Failed to process payment message, abandoning: {}", e.getMessage(), e);
            context.abandon();
        }
    }

    public void processError(ServiceBusErrorContext context) {
        log.error("Service Bus error on entity={}: {}",
                context.getEntityPath(), context.getException().getMessage());
    }

    private UUID parseUuid(String value) {
        if (value == null || value.isBlank()) return UUID.randomUUID();
        try {
            return UUID.fromString(value);
        } catch (IllegalArgumentException e) {
            return UUID.nameUUIDFromBytes(value.getBytes());
        }
    }

    private Instant parseInstant(String value) {
        if (value == null || value.isBlank()) return Instant.now();
        try {
            return Instant.parse(value);
        } catch (Exception e) {
            return Instant.now();
        }
    }

    private String buildAppointmentPayload(AppointmentEventDto dto) {
        try {
            return objectMapper.writeValueAsString(dto);
        } catch (Exception e) {
            return "{}";
        }
    }

    private String buildPaymentPayload(PaymentEventDto dto) {
        try {
            return objectMapper.writeValueAsString(dto);
        } catch (Exception e) {
            return "{}";
        }
    }
}
