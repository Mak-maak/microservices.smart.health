package com.smarthealth.audit.features.storeaudit;

import com.smarthealth.audit.domain.ActorType;
import com.smarthealth.audit.shared.Command;

import java.time.Instant;
import java.util.UUID;

public record StoreAuditEntryCommand(
        UUID eventId,
        String eventType,
        String aggregateType,
        String aggregateId,
        String correlationId,
        String sourceService,
        String actorId,
        ActorType actorType,
        String oldValue,
        String newValue,
        String metadata,
        Instant occurredAt,
        Long version
) implements Command<UUID> {}
