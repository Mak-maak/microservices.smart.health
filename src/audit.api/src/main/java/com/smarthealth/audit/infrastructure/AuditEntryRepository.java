package com.smarthealth.audit.infrastructure;

import com.smarthealth.audit.domain.AuditEntry;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.time.Instant;
import java.util.Optional;
import java.util.UUID;

@Repository
public interface AuditEntryRepository extends JpaRepository<AuditEntry, UUID> {

    boolean existsByEventId(UUID eventId);

    Page<AuditEntry> findByAggregateId(String aggregateId, Pageable pageable);

    Page<AuditEntry> findByCorrelationId(String correlationId, Pageable pageable);

    Page<AuditEntry> findByOccurredAtBetween(Instant from, Instant to, Pageable pageable);

    Page<AuditEntry> findByEventType(String eventType, Pageable pageable);

    Optional<AuditEntry> findTopByOrderByRecordedAtDesc();
}
