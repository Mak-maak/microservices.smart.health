import { Injectable, OnModuleInit, OnModuleDestroy, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { CosmosClient, Container, Database } from '@azure/cosmos';

@Injectable()
export class CosmosService implements OnModuleInit, OnModuleDestroy {
  private readonly logger = new Logger(CosmosService.name);
  private client: CosmosClient;
  private database: Database;
  private container: Container;

  constructor(private readonly configService: ConfigService) {}

  async onModuleInit() {
    const endpoint = this.configService.get<string>('cosmosDb.endpoint');
    const key = this.configService.get<string>('cosmosDb.key');
    const databaseId = this.configService.get<string>('cosmosDb.database');
    const containerId = this.configService.get<string>('cosmosDb.container');

    if (!endpoint || !key) {
      this.logger.warn('Cosmos DB not configured - database operations will fail');
      return;
    }

    this.client = new CosmosClient({ endpoint, key });

    const { database } = await this.client.databases.createIfNotExists({ id: databaseId });
    this.database = database;

    const { container } = await this.database.containers.createIfNotExists({
      id: containerId,
      partitionKey: { paths: ['/aggregateId'] },
    });
    this.container = container;

    this.logger.log(`Cosmos DB connected: ${databaseId}/${containerId}`);
  }

  async onModuleDestroy() {
    if (this.client) {
      this.client.dispose();
    }
  }

  getContainer(): Container | null {
    return this.container || null;
  }

  isReady(): boolean {
    return !!this.container;
  }
}
