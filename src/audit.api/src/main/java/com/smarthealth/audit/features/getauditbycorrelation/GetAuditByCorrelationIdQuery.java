package com.smarthealth.audit.features.getauditbycorrelation;

import com.smarthealth.audit.features.getauditbyaggregate.AuditEntryDto;
import com.smarthealth.audit.shared.Query;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

public record GetAuditByCorrelationIdQuery(
        String correlationId,
        Pageable pageable
) implements Query<Page<AuditEntryDto>> {}
