import { CommandHandler, ICommandHandler } from '@nestjs/cqrs';
import { Logger } from '@nestjs/common';
import { v4 as uuidv4 } from 'uuid';
import { RecordEventCommand } from './record-event.command';
import { EventRecordRepository } from '../../infrastructure/database/event-record.repository';
import { EventRecordDocument } from '../../domain/event-record.document';

@CommandHandler(RecordEventCommand)
export class RecordEventHandler implements ICommandHandler<RecordEventCommand> {
  private readonly logger = new Logger(RecordEventHandler.name);

  constructor(private readonly eventRecordRepository: EventRecordRepository) {}

  async execute(command: RecordEventCommand): Promise<EventRecordDocument> {
    this.logger.log(`Recording event: ${command.eventType} (${command.eventId})`);

    const document: EventRecordDocument = {
      id: uuidv4(),
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

    const saved = await this.eventRecordRepository.save(document);
    this.logger.log(`Event recorded successfully: ${command.eventId}`);
    return saved;
  }
}
