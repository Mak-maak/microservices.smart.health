package com.smarthealth.audit.features.storeaudit;

import com.smarthealth.audit.domain.AuditEntry;
import com.smarthealth.audit.features.getauditbyaggregate.AuditEntryDto;
import org.mapstruct.Mapper;
import org.mapstruct.Mapping;

@Mapper(componentModel = "spring")
public interface AuditEntryMapper {

    @Mapping(target = "actorType", expression = "java(entry.getActorType() != null ? entry.getActorType().name() : null)")
    AuditEntryDto toDto(AuditEntry entry);
}
