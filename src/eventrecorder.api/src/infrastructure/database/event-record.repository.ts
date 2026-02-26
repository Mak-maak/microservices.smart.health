import { Injectable, Logger } from '@nestjs/common';
import { CosmosService } from './cosmos.service';
import { EventRecordDocument } from '../../domain/event-record.document';

@Injectable()
export class EventRecordRepository {
  private readonly logger = new Logger(EventRecordRepository.name);

  constructor(private readonly cosmosService: CosmosService) {}

  async save(document: EventRecordDocument): Promise<EventRecordDocument> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      throw new Error('Cosmos DB not available');
    }

    const alreadyExists = await this.existsByEventId(document.eventId);
    if (alreadyExists) {
      this.logger.log(`Event ${document.eventId} already recorded - skipping (idempotent)`);
      return document;
    }

    const { resource } = await container.items.create(document);
    this.logger.log(`Event recorded: ${document.eventId} (${document.eventType})`);
    return resource as EventRecordDocument;
  }

  private async existsByEventId(eventId: string): Promise<boolean> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      return false;
    }

    const query = {
      query: 'SELECT c.id FROM c WHERE c.eventId = @eventId',
      parameters: [{ name: '@eventId', value: eventId }],
    };

    const { resources } = await container.items.query<{ id: string }>(query).fetchAll();
    return resources.length > 0;
  }

  async findByEventId(eventId: string): Promise<EventRecordDocument | null> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      return null;
    }

    const query = {
      query: 'SELECT * FROM c WHERE c.eventId = @eventId',
      parameters: [{ name: '@eventId', value: eventId }],
    };

    const { resources } = await container.items.query<EventRecordDocument>(query).fetchAll();
    return resources.length > 0 ? resources[0] : null;
  }

  async findByAggregateId(aggregateId: string): Promise<EventRecordDocument[]> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      return [];
    }

    const query = {
      query: 'SELECT * FROM c WHERE c.aggregateId = @aggregateId ORDER BY c.occurredAt ASC',
      parameters: [{ name: '@aggregateId', value: aggregateId }],
    };

    const { resources } = await container.items
      .query<EventRecordDocument>(query, { partitionKey: aggregateId })
      .fetchAll();
    return resources;
  }

  async findByCorrelationId(correlationId: string): Promise<EventRecordDocument[]> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      return [];
    }

    const query = {
      query: 'SELECT * FROM c WHERE c.correlationId = @correlationId ORDER BY c.occurredAt ASC',
      parameters: [{ name: '@correlationId', value: correlationId }],
    };

    const { resources } = await container.items.query<EventRecordDocument>(query).fetchAll();
    return resources;
  }

  async findByEventType(eventType: string): Promise<EventRecordDocument[]> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      return [];
    }

    const query = {
      query: 'SELECT * FROM c WHERE c.eventType = @eventType ORDER BY c.occurredAt DESC',
      parameters: [{ name: '@eventType', value: eventType }],
    };

    const { resources } = await container.items.query<EventRecordDocument>(query).fetchAll();
    return resources;
  }

  async findByDateRange(from: string, to: string): Promise<EventRecordDocument[]> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      return [];
    }

    const query = {
      query: 'SELECT * FROM c WHERE c.occurredAt >= @from AND c.occurredAt <= @to ORDER BY c.occurredAt ASC',
      parameters: [
        { name: '@from', value: from },
        { name: '@to', value: to },
      ],
    };

    const { resources } = await container.items.query<EventRecordDocument>(query).fetchAll();
    return resources;
  }

  async findBySourceService(sourceService: string): Promise<EventRecordDocument[]> {
    const container = this.cosmosService.getContainer();
    if (!container) {
      return [];
    }

    const query = {
      query: 'SELECT * FROM c WHERE c.sourceService = @sourceService ORDER BY c.occurredAt DESC',
      parameters: [{ name: '@sourceService', value: sourceService }],
    };

    const { resources } = await container.items.query<EventRecordDocument>(query).fetchAll();
    return resources;
  }
}
