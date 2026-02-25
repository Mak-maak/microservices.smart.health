package com.smarthealth.audit.messaging;

import com.azure.messaging.servicebus.ServiceBusClientBuilder;
import com.azure.messaging.servicebus.ServiceBusProcessorClient;
import com.azure.messaging.servicebus.models.ServiceBusReceiveMode;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.event.ContextRefreshedEvent;
import org.springframework.context.event.EventListener;

@Slf4j
@Configuration
@RequiredArgsConstructor
@ConditionalOnProperty(name = "azure.servicebus.connection-string", matchIfMissing = false)
public class MessagingConfig {

    @Value("${azure.servicebus.connection-string}")
    private String connectionString;

    @Value("${azure.servicebus.appointments-topic:appointments-events}")
    private String appointmentsTopic;

    @Value("${azure.servicebus.appointments-subscription:audit-subscription}")
    private String appointmentsSubscription;

    @Value("${azure.servicebus.payments-topic:payments-events}")
    private String paymentsTopic;

    @Value("${azure.servicebus.payments-subscription:audit-subscription}")
    private String paymentsSubscription;

    private final AuditEventListener auditEventListener;

    @Bean(name = "appointmentsProcessorClient", destroyMethod = "close")
    public ServiceBusProcessorClient appointmentsProcessorClient() {
        log.info("Creating Service Bus processor for topic={} subscription={}", appointmentsTopic, appointmentsSubscription);
        return new ServiceBusClientBuilder()
                .connectionString(connectionString)
                .processor()
                .topicName(appointmentsTopic)
                .subscriptionName(appointmentsSubscription)
                .receiveMode(ServiceBusReceiveMode.PEEK_LOCK)
                .processMessage(auditEventListener::processAppointmentMessage)
                .processError(auditEventListener::processError)
                .buildProcessorClient();
    }

    @Bean(name = "paymentsProcessorClient", destroyMethod = "close")
    public ServiceBusProcessorClient paymentsProcessorClient() {
        log.info("Creating Service Bus processor for topic={} subscription={}", paymentsTopic, paymentsSubscription);
        return new ServiceBusClientBuilder()
                .connectionString(connectionString)
                .processor()
                .topicName(paymentsTopic)
                .subscriptionName(paymentsSubscription)
                .receiveMode(ServiceBusReceiveMode.PEEK_LOCK)
                .processMessage(auditEventListener::processPaymentMessage)
                .processError(auditEventListener::processError)
                .buildProcessorClient();
    }

    @EventListener(ContextRefreshedEvent.class)
    public void startProcessors(ContextRefreshedEvent event) {
        ServiceBusProcessorClient appointments =
                event.getApplicationContext().getBean("appointmentsProcessorClient", ServiceBusProcessorClient.class);
        ServiceBusProcessorClient payments =
                event.getApplicationContext().getBean("paymentsProcessorClient", ServiceBusProcessorClient.class);

        if (!appointments.isRunning()) {
            log.info("Starting appointments Service Bus processor");
            appointments.start();
        }
        if (!payments.isRunning()) {
            log.info("Starting payments Service Bus processor");
            payments.start();
        }
    }
}
