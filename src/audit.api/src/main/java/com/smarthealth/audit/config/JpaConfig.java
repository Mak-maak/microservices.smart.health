package com.smarthealth.audit.config;

import org.springframework.context.annotation.Configuration;

@Configuration
public class JpaConfig {
    // Hibernate/JPA settings are configured via application.yml
    // PostgreSQL dialect with JSONB support enabled via:
    //   spring.jpa.properties.hibernate.dialect=org.hibernate.dialect.PostgreSQLDialect
}
