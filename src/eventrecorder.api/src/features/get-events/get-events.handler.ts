import { IQueryHandler, QueryHandler } from '@nestjs/cqrs';
import { Logger } from '@nestjs/common';
import { GetEventsByAggregateQuery } from './get-events-by-aggregate.query';
import { GetEventsByCorrelationQuery } from './get-events-by-correlation.query';
import { GetEventsByDateRangeQuery } from './get-events-by-date-range.query';
import { GetEventsByTypeQuery } from './get-events-by-type.query';
import { EventRecordRepository } from '../../infrastructure/database/event-record.repository';
import { EventRecordDocument } from '../../domain/event-record.document';

@QueryHandler(GetEventsByAggregateQuery)
export class GetEventsByAggregateHandler implements IQueryHandler<GetEventsByAggregateQuery> {
  private readonly logger = new Logger(GetEventsByAggregateHandler.name);

  constructor(private readonly repository: EventRecordRepository) {}

  async execute(query: GetEventsByAggregateQuery): Promise<EventRecordDocument[]> {
    this.logger.log(`Getting events for aggregateId: ${query.aggregateId}`);
    return this.repository.findByAggregateId(query.aggregateId);
  }
}

@QueryHandler(GetEventsByCorrelationQuery)
export class GetEventsByCorrelationHandler implements IQueryHandler<GetEventsByCorrelationQuery> {
  private readonly logger = new Logger(GetEventsByCorrelationHandler.name);

  constructor(private readonly repository: EventRecordRepository) {}

  async execute(query: GetEventsByCorrelationQuery): Promise<EventRecordDocument[]> {
    this.logger.log(`Getting events for correlationId: ${query.correlationId}`);
    return this.repository.findByCorrelationId(query.correlationId);
  }
}

@QueryHandler(GetEventsByDateRangeQuery)
export class GetEventsByDateRangeHandler implements IQueryHandler<GetEventsByDateRangeQuery> {
  private readonly logger = new Logger(GetEventsByDateRangeHandler.name);

  constructor(private readonly repository: EventRecordRepository) {}

  async execute(query: GetEventsByDateRangeQuery): Promise<EventRecordDocument[]> {
    this.logger.log(`Getting events from ${query.from} to ${query.to}`);
    return this.repository.findByDateRange(query.from, query.to);
  }
}

@QueryHandler(GetEventsByTypeQuery)
export class GetEventsByTypeHandler implements IQueryHandler<GetEventsByTypeQuery> {
  private readonly logger = new Logger(GetEventsByTypeHandler.name);

  constructor(private readonly repository: EventRecordRepository) {}

  async execute(query: GetEventsByTypeQuery): Promise<EventRecordDocument[]> {
    this.logger.log(`Getting events of type: ${query.eventType}`);
    return this.repository.findByEventType(query.eventType);
  }
}
