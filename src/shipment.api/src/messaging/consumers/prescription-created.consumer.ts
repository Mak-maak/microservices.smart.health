import { Injectable, Logger, OnModuleInit } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { CommandBus } from '@nestjs/cqrs';
import { ServiceBusReceiver, ServiceBusReceivedMessage, ProcessErrorArgs } from '@azure/service-bus';
import { ServiceBusService } from '../../infrastructure/messaging/service-bus.service';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { CreateShipmentCommand } from '../../features/create-shipment/create-shipment.command';
import { v4 as uuidv4 } from 'uuid';

@Injectable()
export class PrescriptionCreatedConsumer implements OnModuleInit {
  private readonly logger = new Logger(PrescriptionCreatedConsumer.name);
  private receiver: ServiceBusReceiver;

  constructor(
    private readonly serviceBusService: ServiceBusService,
    private readonly configService: ConfigService,
    private readonly commandBus: CommandBus,
    private readonly shipmentRepository: ShipmentRepository,
  ) {}

  onModuleInit() {
    const client = this.serviceBusService.getClient();
    if (!client) {
      this.logger.warn('Service Bus not available - PrescriptionCreated consumer disabled');
      return;
    }

    const topicName = this.configService.get<string>('serviceBus.topics.prescriptionCreated');
    const subscriptionName = this.configService.get<string>('serviceBus.subscriptionName');

    this.receiver = client.createReceiver(topicName, subscriptionName);

    this.receiver.subscribe({
      processMessage: this.handleMessage.bind(this),
      processError: this.handleError.bind(this),
    });

    this.logger.log(`Subscribed to ${topicName}/${subscriptionName}`);
  }

  private async handleMessage(message: ServiceBusReceivedMessage): Promise<void> {
    const messageId = message.messageId as string;
    const correlationId = (message.correlationId as string) || uuidv4();

    this.logger.log(`Received PrescriptionCreated message: ${messageId}`);

    const alreadyProcessed = await this.shipmentRepository.isMessageProcessed(messageId);
    if (alreadyProcessed) {
      this.logger.log(`Message ${messageId} already processed - skipping`);
      await this.receiver.completeMessage(message);
      return;
    }

    const body = message.body;

    try {
      const command = new CreateShipmentCommand(
        body.prescriptionId,
        body.patientId,
        body.pharmacyId || uuidv4(),
        body.medications || [],
        body.address || { street: '', city: '', state: '', postalCode: '', country: '' },
        correlationId,
        messageId,
      );

      await this.commandBus.execute(command);
      await this.receiver.completeMessage(message);
      this.logger.log(`PrescriptionCreated processed successfully: ${messageId}`);
    } catch (error) {
      this.logger.error(`Failed to process PrescriptionCreated: ${error.message}`);
      await this.receiver.deadLetterMessage(message, {
        deadLetterReason: 'ProcessingFailed',
        deadLetterErrorDescription: error.message,
      });
    }
  }

  private async handleError(args: ProcessErrorArgs): Promise<void> {
    this.logger.error(`Service Bus error: ${args.error.message}`);
  }
}
