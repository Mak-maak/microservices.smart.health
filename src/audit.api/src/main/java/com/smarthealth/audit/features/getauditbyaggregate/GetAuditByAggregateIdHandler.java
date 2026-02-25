package com.smarthealth.audit.features.getauditbyaggregate;

import com.smarthealth.audit.features.storeaudit.AuditEntryMapper;
import com.smarthealth.audit.infrastructure.AuditEntryRepository;
import com.smarthealth.audit.shared.QueryHandler;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class GetAuditByAggregateIdHandler
        implements QueryHandler<GetAuditByAggregateIdQuery, Page<AuditEntryDto>> {

    private final AuditEntryRepository repository;
    private final AuditEntryMapper mapper;

    @Override
    @Transactional(readOnly = true)
    public Page<AuditEntryDto> handle(GetAuditByAggregateIdQuery query) {
        return repository.findByAggregateId(query.aggregateId(), query.pageable())
                .map(mapper::toDto);
    }
}
