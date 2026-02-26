import {
  GetEventsByAggregateHandler,
  GetEventsByCorrelationHandler,
  GetEventsByDateRangeHandler,
  GetEventsByTypeHandler,
} from './get-events.handler';
import { GetEventsByAggregateQuery } from './get-events-by-aggregate.query';
import { GetEventsByCorrelationQuery } from './get-events-by-correlation.query';
import { GetEventsByDateRangeQuery } from './get-events-by-date-range.query';
import { GetEventsByTypeQuery } from './get-events-by-type.query';
import { EventRecordRepository } from '../../infrastructure/database/event-record.repository';

const mockDoc = {
  id: 'doc-1',
  eventId: 'evt-1',
  eventType: 'AppointmentCreated',
  aggregateType: 'Appointment',
  aggregateId: 'agg-1',
  correlationId: 'corr-1',
  sourceService: 'appointments-service',
  actorId: 'user-1',
  actorType: 'Patient',
  payload: {},
  metadata: {},
  occurredAt: '2024-01-01T00:00:00.000Z',
  recordedAt: '2024-01-01T00:00:01.000Z',
  version: 1,
};

function makeRepository(): jest.Mocked<EventRecordRepository> {
  return {
    save: jest.fn(),
    findByEventId: jest.fn(),
    findByAggregateId: jest.fn(),
    findByCorrelationId: jest.fn(),
    findByEventType: jest.fn(),
    findByDateRange: jest.fn(),
    findBySourceService: jest.fn(),
  } as any;
}

describe('GetEventsByAggregateHandler', () => {
  it('should return events by aggregateId', async () => {
    const repo = makeRepository();
    repo.findByAggregateId.mockResolvedValue([mockDoc]);
    const handler = new GetEventsByAggregateHandler(repo);
    const result = await handler.execute(new GetEventsByAggregateQuery('agg-1'));
    expect(result).toHaveLength(1);
    expect(result[0].aggregateId).toBe('agg-1');
  });
});

describe('GetEventsByCorrelationHandler', () => {
  it('should return events by correlationId', async () => {
    const repo = makeRepository();
    repo.findByCorrelationId.mockResolvedValue([mockDoc]);
    const handler = new GetEventsByCorrelationHandler(repo);
    const result = await handler.execute(new GetEventsByCorrelationQuery('corr-1'));
    expect(result).toHaveLength(1);
    expect(result[0].correlationId).toBe('corr-1');
  });
});

describe('GetEventsByDateRangeHandler', () => {
  it('should return events by date range', async () => {
    const repo = makeRepository();
    repo.findByDateRange.mockResolvedValue([mockDoc]);
    const handler = new GetEventsByDateRangeHandler(repo);
    const result = await handler.execute(new GetEventsByDateRangeQuery('2024-01-01', '2024-12-31'));
    expect(result).toHaveLength(1);
  });
});

describe('GetEventsByTypeHandler', () => {
  it('should return events by eventType', async () => {
    const repo = makeRepository();
    repo.findByEventType.mockResolvedValue([mockDoc]);
    const handler = new GetEventsByTypeHandler(repo);
    const result = await handler.execute(new GetEventsByTypeQuery('AppointmentCreated'));
    expect(result).toHaveLength(1);
    expect(result[0].eventType).toBe('AppointmentCreated');
  });
});
