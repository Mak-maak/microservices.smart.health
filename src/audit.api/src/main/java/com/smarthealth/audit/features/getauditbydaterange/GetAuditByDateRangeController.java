package com.smarthealth.audit.features.getauditbydaterange;

import com.smarthealth.audit.features.getauditbyaggregate.AuditEntryDto;
import com.smarthealth.audit.features.getauditbyeventtype.GetAuditByEventTypeHandler;
import com.smarthealth.audit.features.getauditbyeventtype.GetAuditByEventTypeQuery;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.web.PageableDefault;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.time.Instant;

@RestController
@RequestMapping("/api/audit")
@RequiredArgsConstructor
@Tag(name = "Audit", description = "Audit trail endpoints")
@SecurityRequirement(name = "bearerAuth")
public class GetAuditByDateRangeController {

    private final GetAuditByDateRangeHandler dateRangeHandler;
    private final GetAuditByEventTypeHandler eventTypeHandler;

    @GetMapping
    @PreAuthorize("hasRole('AUDIT_READ')")
    @Operation(summary = "Get audit entries by date range or event type")
    public ResponseEntity<Page<AuditEntryDto>> getAudit(
            @RequestParam(required = false) String eventType,
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant from,
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant to,
            @PageableDefault(size = 20, sort = "occurredAt") Pageable pageable
    ) {
        if (eventType != null && !eventType.isBlank()) {
            Page<AuditEntryDto> result = eventTypeHandler.handle(
                    new GetAuditByEventTypeQuery(eventType, pageable));
            return ResponseEntity.ok(result);
        }

        Instant effectiveFrom = from != null ? from : Instant.EPOCH;
        Instant effectiveTo = to != null ? to : Instant.now();

        Page<AuditEntryDto> result = dateRangeHandler.handle(
                new GetAuditByDateRangeQuery(effectiveFrom, effectiveTo, pageable));
        return ResponseEntity.ok(result);
    }
}
