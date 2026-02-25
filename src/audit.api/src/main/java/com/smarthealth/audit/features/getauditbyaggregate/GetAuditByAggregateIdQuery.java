package com.smarthealth.audit.features.getauditbyaggregate;

import com.smarthealth.audit.shared.Query;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

public record GetAuditByAggregateIdQuery(
        String aggregateId,
        Pageable pageable
) implements Query<Page<AuditEntryDto>> {}
