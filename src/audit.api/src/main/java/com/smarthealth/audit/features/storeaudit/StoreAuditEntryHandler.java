package com.smarthealth.audit.features.storeaudit;

import com.smarthealth.audit.domain.AuditEntry;
import com.smarthealth.audit.infrastructure.AuditEntryRepository;
import com.smarthealth.audit.infrastructure.HashService;
import com.smarthealth.audit.shared.CommandHandler;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.UUID;

@Slf4j
@Service
@RequiredArgsConstructor
public class StoreAuditEntryHandler implements CommandHandler<StoreAuditEntryCommand, UUID> {

    private final AuditEntryRepository repository;
    private final HashService hashService;

    @Override
    @Transactional
    public UUID handle(StoreAuditEntryCommand command) {
        if (repository.existsByEventId(command.eventId())) {
            log.debug("Idempotency check: event {} already stored, skipping", command.eventId());
            return null;
        }

        String previousHash = repository.findTopByOrderByRecordedAtDesc()
                .map(AuditEntry::getHash)
                .orElse("GENESIS");

        String hash = hashService.computeHash(
                previousHash,
                command.eventId(),
                command.eventType(),
                command.aggregateId(),
                command.occurredAt()
        );

        AuditEntry entry = new AuditEntry(
                command.eventId(),
                command.eventType(),
                command.aggregateType(),
                command.aggregateId(),
                command.correlationId(),
                command.sourceService(),
                command.actorId(),
                command.actorType(),
                command.oldValue(),
                command.newValue(),
                command.metadata(),
                command.occurredAt(),
                command.version(),
                hash,
                previousHash
        );

        AuditEntry saved = repository.save(entry);
        log.info("Stored audit entry id={} eventType={} aggregateId={}",
                saved.getId(), saved.getEventType(), saved.getAggregateId());
        return saved.getId();
    }
}
