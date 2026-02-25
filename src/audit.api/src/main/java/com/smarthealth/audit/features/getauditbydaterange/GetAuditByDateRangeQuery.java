package com.smarthealth.audit.features.getauditbydaterange;

import com.smarthealth.audit.features.getauditbyaggregate.AuditEntryDto;
import com.smarthealth.audit.shared.Query;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

import java.time.Instant;

public record GetAuditByDateRangeQuery(
        Instant from,
        Instant to,
        Pageable pageable
) implements Query<Page<AuditEntryDto>> {}
