package com.smarthealth.audit.features.getauditbycorrelation;

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
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/audit")
@RequiredArgsConstructor
@Tag(name = "Audit", description = "Audit trail endpoints")
@SecurityRequirement(name = "bearerAuth")
public class GetAuditByCorrelationIdController {

    private final GetAuditByCorrelationIdHandler handler;

    @GetMapping("/correlation/{correlationId}")
    @PreAuthorize("hasRole('AUDIT_READ')")
    @Operation(summary = "Get audit entries by correlation ID")
    public ResponseEntity<Page<AuditEntryDto>> getByCorrelationId(
            @PathVariable String correlationId,
            @PageableDefault(size = 20, sort = "occurredAt") Pageable pageable
    ) {
        Page<AuditEntryDto> result = handler.handle(new GetAuditByCorrelationIdQuery(correlationId, pageable));
        return ResponseEntity.ok(result);
    }
}
