package com.smarthealth.audit.features.getauditbyeventtype;

import com.smarthealth.audit.features.getauditbyaggregate.AuditEntryDto;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.web.PageableDefault;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/audit/events")
@RequiredArgsConstructor
@Tag(name = "Audit", description = "Audit trail endpoints")
@SecurityRequirement(name = "bearerAuth")
public class GetAuditByEventTypeController {

    private final GetAuditByEventTypeHandler handler;

    @GetMapping
    @PreAuthorize("hasRole('AUDIT_READ')")
    @Operation(summary = "Get audit entries by event type")
    public ResponseEntity<Page<AuditEntryDto>> getByEventType(
            @RequestParam String eventType,
            @PageableDefault(size = 20, sort = "occurredAt") Pageable pageable
    ) {
        Page<AuditEntryDto> result = handler.handle(new GetAuditByEventTypeQuery(eventType, pageable));
        return ResponseEntity.ok(result);
    }
}
