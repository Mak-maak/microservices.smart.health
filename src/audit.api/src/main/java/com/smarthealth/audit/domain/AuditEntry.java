package com.smarthealth.audit.domain;

import jakarta.persistence.*;
import lombok.Getter;

import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "audit_entries")
@Getter
public class AuditEntry {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Column(name = "event_id", nullable = false, unique = true)
    private UUID eventId;

    @Column(name = "event_type", nullable = false)
    private String eventType;

    @Column(name = "aggregate_type", nullable = false)
    private String aggregateType;

    @Column(name = "aggregate_id", nullable = false)
    private String aggregateId;

    @Column(name = "correlation_id")
    private String correlationId;

    @Column(name = "source_service", nullable = false)
    private String sourceService;

    @Column(name = "actor_id")
    private String actorId;

    @Enumerated(EnumType.STRING)
    @Column(name = "actor_type", nullable = false)
    private ActorType actorType;

    @Column(name = "old_value", columnDefinition = "jsonb")
    private String oldValue;

    @Column(name = "new_value", columnDefinition = "jsonb")
    private String newValue;

    @Column(name = "metadata", columnDefinition = "jsonb")
    private String metadata;

    @Column(name = "occurred_at", nullable = false)
    private Instant occurredAt;

    @Column(name = "recorded_at", nullable = false)
    private Instant recordedAt;

    @Column(name = "version", nullable = false)
    private Long version;

    @Column(name = "hash", nullable = false)
    private String hash;

    @Column(name = "previous_hash")
    private String previousHash;

    protected AuditEntry() {
        // no-args constructor for JPA
    }

    public AuditEntry(
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
            Long version,
            String hash,
            String previousHash
    ) {
        this.eventId = eventId;
        this.eventType = eventType;
        this.aggregateType = aggregateType;
        this.aggregateId = aggregateId;
        this.correlationId = correlationId;
        this.sourceService = sourceService;
        this.actorId = actorId;
        this.actorType = actorType;
        this.oldValue = oldValue;
        this.newValue = newValue;
        this.metadata = metadata;
        this.occurredAt = occurredAt;
        this.version = version;
        this.hash = hash;
        this.previousHash = previousHash;
    }

    @PrePersist
    protected void onPersist() {
        this.recordedAt = Instant.now();
    }
}
