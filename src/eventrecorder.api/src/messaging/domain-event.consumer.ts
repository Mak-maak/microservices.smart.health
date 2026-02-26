import { Injectable, Logger, OnModuleInit, OnModuleDestroy } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { CommandBus } from '@nestjs/cqrs';
import { ServiceBusReceiver, ServiceBusReceivedMessage, ProcessErrorArgs } from '@azure/service-bus';
import { ServiceBusService } from '../infrastructure/messaging/service-bus.service';
import { RecordEventCommand } from '../features/record-event/record-event.command';
import { v4 as uuidv4 } from 'uuid';

@Injectable()
export class DomainEventConsumer implements OnModuleInit, OnModuleDestroy {
  private readonly logger = new Logger(DomainEventConsumer.name);
  private readonly receivers: ServiceBusReceiver[] = [];

  private readonly SUPPORTED_TOPICS = [
    'appointmentCreated',
    'paymentCompleted',
    'paymentFailed',
    'prescriptionCreated',
    'shipmentDispatched',
  ];

  constructor(
    private readonly serviceBusService: ServiceBusService,
    private readonly configService: ConfigService,
    private readonly commandBus: CommandBus,
  ) {}

  onModuleInit() {
    const client = this.serviceBusService.getClient();
    if (!client) {
      this.logger.warn('Service Bus not available - domain event consumer disabled');
      return;
    }

    const subscriptionName = this.configService.get<string>('serviceBus.subscriptionName');

    for (const topicKey of this.SUPPORTED_TOPICS) {
      const topicName = this.configService.get<string>(`serviceBus.topics.${topicKey}`);
      if (!topicName) continue;

      try {
        const receiver = client.createReceiver(topicName, subscriptionName);
        receiver.subscribe({
          processMessage: (msg) => this.handleMessage(msg, topicKey),
          processError: this.handleError.bind(this),
        });
        this.receivers.push(receiver);
        this.logger.log(`Subscribed to ${topicName}/${subscriptionName}`);
      } catch (error) {
        this.logger.error(`Failed to subscribe to ${topicName}: ${error.message}`);
      }
    }
  }

  async onModuleDestroy() {
    for (const receiver of this.receivers) {
      try {
        await receiver.close();
      } catch (error) {
        this.logger.error(`Error closing receiver: ${error.message}`);
      }
    }
  }

  private async handleMessage(message: ServiceBusReceivedMessage, topicKey: string): Promise<void> {
    const messageId = (message.messageId as string) || uuidv4();
    const correlationId = (message.correlationId as string) || uuidv4();
    const body = message.body || {};

    const eventType = (message.subject as string) ||
      (message.applicationProperties?.eventType as string) ||
      this.topicKeyToEventType(topicKey);

    this.logger.log(`Received ${eventType} message: ${messageId}`);

    try {
      const command = new RecordEventCommand(
        body.eventId || messageId,
        eventType,
        body.aggregateType || this.inferAggregateType(eventType),
        body.aggregateId || body.id || uuidv4(),
        correlationId,
        (body.sourceService as string) || (message.applicationProperties?.sourceService as string) || 'unknown',
        body.actorId || '',
        body.actorType || '',
        body.payload || body,
        {
          messageId,
          topic: this.configService.get<string>(`serviceBus.topics.${topicKey}`),
          ...(message.applicationProperties || {}),
        },
        body.occurredAt || (message.enqueuedTimeUtc as Date)?.toISOString() || new Date().toISOString(),
        body.version || 1,
      );

      await this.commandBus.execute(command);

      const receiver = this.receivers.find((r) => r.entityPath?.includes(
        this.configService.get<string>(`serviceBus.topics.${topicKey}`) || '',
      ));
      if (receiver) {
        await receiver.completeMessage(message);
      }

      this.logger.log(`Domain event processed: ${eventType} (${messageId})`);
    } catch (error) {
      this.logger.error(`Failed to process ${eventType}: ${error.message}`);
    }
  }

  private async handleError(args: ProcessErrorArgs): Promise<void> {
    this.logger.error(`Service Bus error on ${args.entityPath}: ${args.error.message}`);
  }

  private topicKeyToEventType(topicKey: string): string {
    const map: Record<string, string> = {
      appointmentCreated: 'AppointmentCreated',
      paymentCompleted: 'PaymentCompleted',
      paymentFailed: 'PaymentFailed',
      prescriptionCreated: 'PrescriptionCreated',
      shipmentDispatched: 'ShipmentDispatched',
    };
    return map[topicKey] || topicKey;
  }

  private inferAggregateType(eventType: string): string {
    if (eventType.startsWith('Appointment')) return 'Appointment';
    if (eventType.startsWith('Payment')) return 'Payment';
    if (eventType.startsWith('Prescription')) return 'Prescription';
    if (eventType.startsWith('Shipment')) return 'Shipment';
    return 'Unknown';
  }
}
