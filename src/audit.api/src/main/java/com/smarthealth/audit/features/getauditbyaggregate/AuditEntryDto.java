package com.smarthealth.audit.features.getauditbyaggregate;

import java.time.Instant;
import java.util.UUID;

public record AuditEntryDto(
        UUID id,
        UUID eventId,
        String eventType,
        String aggregateType,
        String aggregateId,
        String correlationId,
        String sourceService,
        String actorId,
        String actorType,
        String oldValue,
        String newValue,
        String metadata,
        Instant occurredAt,
        Instant recordedAt,
        Long version,
        String hash,
        String previousHash
) {}
