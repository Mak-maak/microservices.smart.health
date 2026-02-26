import { Controller, Get, Param, Query, Logger, BadRequestException } from '@nestjs/common';
import { QueryBus } from '@nestjs/cqrs';
import { GetEventsByAggregateQuery } from './get-events-by-aggregate.query';
import { GetEventsByCorrelationQuery } from './get-events-by-correlation.query';
import { GetEventsByDateRangeQuery } from './get-events-by-date-range.query';
import { GetEventsByTypeQuery } from './get-events-by-type.query';

@Controller('api/events')
export class GetEventsController {
  private readonly logger = new Logger(GetEventsController.name);

  constructor(private readonly queryBus: QueryBus) {}

  @Get('aggregate/:aggregateId')
  async getByAggregate(@Param('aggregateId') aggregateId: string) {
    return this.queryBus.execute(new GetEventsByAggregateQuery(aggregateId));
  }

  @Get('correlation/:correlationId')
  async getByCorrelation(@Param('correlationId') correlationId: string) {
    return this.queryBus.execute(new GetEventsByCorrelationQuery(correlationId));
  }

  @Get('type/:eventType')
  async getByType(@Param('eventType') eventType: string) {
    return this.queryBus.execute(new GetEventsByTypeQuery(eventType));
  }

  @Get('date-range')
  async getByDateRange(
    @Query('from') from: string,
    @Query('to') to: string,
  ) {
    if (!from || !to) {
      throw new BadRequestException('Query parameters "from" and "to" are required (ISO 8601)');
    }
    if (isNaN(Date.parse(from)) || isNaN(Date.parse(to))) {
      throw new BadRequestException('"from" and "to" must be valid ISO 8601 date strings');
    }
    return this.queryBus.execute(new GetEventsByDateRangeQuery(from, to));
  }
}
