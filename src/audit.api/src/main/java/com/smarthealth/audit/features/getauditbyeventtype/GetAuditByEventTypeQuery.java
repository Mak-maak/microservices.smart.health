package com.smarthealth.audit.features.getauditbyeventtype;

import com.smarthealth.audit.features.getauditbyaggregate.AuditEntryDto;
import com.smarthealth.audit.shared.Query;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

public record GetAuditByEventTypeQuery(
        String eventType,
        Pageable pageable
) implements Query<Page<AuditEntryDto>> {}
