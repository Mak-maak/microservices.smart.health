import { RecordEventHandler } from './record-event.handler';
import { RecordEventCommand } from './record-event.command';
import { EventRecordRepository } from '../../infrastructure/database/event-record.repository';

describe('RecordEventHandler', () => {
  let handler: RecordEventHandler;
  let repository: jest.Mocked<EventRecordRepository>;

  beforeEach(() => {
    repository = {
      save: jest.fn(),
      findByEventId: jest.fn(),
      findByAggregateId: jest.fn(),
      findByCorrelationId: jest.fn(),
      findByEventType: jest.fn(),
      findByDateRange: jest.fn(),
      findBySourceService: jest.fn(),
    } as any;

    handler = new RecordEventHandler(repository);
  });

  it('should record an event and return the saved document', async () => {
    const command = new RecordEventCommand(
      'event-id-123',
      'AppointmentCreated',
      'Appointment',
      'agg-456',
      'corr-789',
      'appointments-service',
      'user-001',
      'Patient',
      { appointmentId: 'agg-456' },
      {},
      new Date().toISOString(),
      1,
    );

    const savedDoc = {
      id: 'doc-id',
      eventId: command.eventId,
      eventType: command.eventType,
      aggregateType: command.aggregateType,
      aggregateId: command.aggregateId,
      correlationId: command.correlationId,
      sourceService: command.sourceService,
      actorId: command.actorId,
      actorType: command.actorType,
      payload: command.payload,
      metadata: command.metadata,
      occurredAt: command.occurredAt,
      recordedAt: new Date().toISOString(),
      version: command.version,
    };

    repository.save.mockResolvedValue(savedDoc);

    const result = await handler.execute(command);

    expect(repository.save).toHaveBeenCalledTimes(1);
    expect(result.eventId).toBe(command.eventId);
    expect(result.eventType).toBe('AppointmentCreated');
  });

  it('should set recordedAt to current time', async () => {
    const command = new RecordEventCommand(
      'event-id-456',
      'PaymentCompleted',
      'Payment',
      'agg-pay-1',
      'corr-pay-1',
      'payments-service',
      '',
      '',
      {},
      {},
      new Date().toISOString(),
      1,
    );

    const savedDoc = {
      id: 'doc-id-2',
      eventId: command.eventId,
      eventType: command.eventType,
      aggregateType: command.aggregateType,
      aggregateId: command.aggregateId,
      correlationId: command.correlationId,
      sourceService: command.sourceService,
      actorId: command.actorId,
      actorType: command.actorType,
      payload: command.payload,
      metadata: command.metadata,
      occurredAt: command.occurredAt,
      recordedAt: new Date().toISOString(),
      version: command.version,
    };

    repository.save.mockResolvedValue(savedDoc);

    const result = await handler.execute(command);
    expect(result.recordedAt).toBeDefined();
  });
});
