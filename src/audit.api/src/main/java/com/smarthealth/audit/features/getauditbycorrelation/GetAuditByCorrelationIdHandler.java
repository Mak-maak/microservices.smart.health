package com.smarthealth.audit.features.getauditbycorrelation;

import com.smarthealth.audit.features.getauditbyaggregate.AuditEntryDto;
import com.smarthealth.audit.features.storeaudit.AuditEntryMapper;
import com.smarthealth.audit.infrastructure.AuditEntryRepository;
import com.smarthealth.audit.shared.QueryHandler;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class GetAuditByCorrelationIdHandler
        implements QueryHandler<GetAuditByCorrelationIdQuery, Page<AuditEntryDto>> {

    private final AuditEntryRepository repository;
    private final AuditEntryMapper mapper;

    @Override
    @Transactional(readOnly = true)
    public Page<AuditEntryDto> handle(GetAuditByCorrelationIdQuery query) {
        return repository.findByCorrelationId(query.correlationId(), query.pageable())
                .map(mapper::toDto);
    }
}
