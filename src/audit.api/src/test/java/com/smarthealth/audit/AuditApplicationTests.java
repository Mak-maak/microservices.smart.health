package com.smarthealth.audit;

import com.azure.messaging.servicebus.ServiceBusProcessorClient;
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.TestPropertySource;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.NONE)
@TestPropertySource(properties = {
    "spring.datasource.url=jdbc:h2:mem:testdb;DB_CLOSE_DELAY=-1",
    "spring.datasource.driver-class-name=org.h2.Driver",
    "spring.datasource.username=sa",
    "spring.datasource.password=",
    "spring.jpa.hibernate.ddl-auto=create-drop",
    "spring.jpa.properties.hibernate.dialect=org.hibernate.dialect.H2Dialect",
    "spring.flyway.enabled=false"
})
class AuditApplicationTests {

    @MockBean(name = "appointmentsProcessorClient")
    private ServiceBusProcessorClient appointmentsProcessorClient;

    @MockBean(name = "paymentsProcessorClient")
    private ServiceBusProcessorClient paymentsProcessorClient;

    @Test
    void contextLoads() {
        // Validates that the Spring context starts successfully
    }
}
