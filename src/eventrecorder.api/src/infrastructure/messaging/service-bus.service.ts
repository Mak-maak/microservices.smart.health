import { Injectable, OnModuleInit, OnModuleDestroy, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { ServiceBusClient } from '@azure/service-bus';

@Injectable()
export class ServiceBusService implements OnModuleInit, OnModuleDestroy {
  private readonly logger = new Logger(ServiceBusService.name);
  private client: ServiceBusClient;
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
    if (this.client) {
      await this.client.close();
    }
  }

  getClient(): ServiceBusClient | null {
    return this.client || null;
  }
}
