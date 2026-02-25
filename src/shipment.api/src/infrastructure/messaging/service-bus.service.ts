import { Injectable, OnModuleInit, OnModuleDestroy, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import {
  ServiceBusClient,
  ServiceBusSender,
  ServiceBusMessage,
} from '@azure/service-bus';
import { v4 as uuidv4 } from 'uuid';

@Injectable()
export class ServiceBusService implements OnModuleInit, OnModuleDestroy {
  private readonly logger = new Logger(ServiceBusService.name);
  private client: ServiceBusClient;
  private senders: Map<string, ServiceBusSender> = new Map();
  private readonly connectionString: string;

  constructor(private readonly configService: ConfigService) {
    this.connectionString = this.configService.get<string>('serviceBus.connectionString');
  }

  onModuleInit() {
    if (this.connectionString) {
      this.client = new ServiceBusClient(this.connectionString);
      this.logger.log('Service Bus client initialized');
    } else {
      this.logger.warn('Service Bus connection string not configured - messaging disabled');
    }
  }

  async onModuleDestroy() {
    for (const sender of this.senders.values()) {
      await sender.close();
    }
    if (this.client) {
      await this.client.close();
    }
  }

  async publishEvent(topicName: string, eventType: string, payload: any, correlationId?: string): Promise<void> {
    if (!this.client) {
      this.logger.warn(`Service Bus not configured. Skipping publish to ${topicName}`);
      return;
    }

    try {
      if (!this.senders.has(topicName)) {
        this.senders.set(topicName, this.client.createSender(topicName));
      }
      const sender = this.senders.get(topicName);

      const message: ServiceBusMessage = {
        messageId: uuidv4(),
        correlationId: correlationId,
        subject: eventType,
        body: payload,
        contentType: 'application/json',
        applicationProperties: {
          eventType,
          sourceService: 'shipment-service',
          occurredAt: new Date().toISOString(),
        },
      };

      await sender.sendMessages(message);
      this.logger.log(`Published ${eventType} to ${topicName}`);
    } catch (error) {
      this.logger.error(`Failed to publish ${eventType} to ${topicName}: ${error.message}`);
      throw error;
    }
  }

  getClient(): ServiceBusClient | null {
    return this.client || null;
  }
}
