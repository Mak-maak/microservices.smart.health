import { CommandHandler, ICommandHandler } from '@nestjs/cqrs';
import { Logger } from '@nestjs/common';
import { v4 as uuidv4 } from 'uuid';
import { CreateShipmentCommand } from './create-shipment.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { Shipment } from '../../domain/shipment.entity';
import { ShipmentStatus } from '../../domain/shipment-status.enum';

@CommandHandler(CreateShipmentCommand)
export class CreateShipmentHandler implements ICommandHandler<CreateShipmentCommand> {
  private readonly logger = new Logger(CreateShipmentHandler.name);

  constructor(
    private readonly shipmentRepository: ShipmentRepository,
    private readonly eventPublisher: ShipmentEventPublisher,
  ) {}

  async execute(command: CreateShipmentCommand): Promise<Shipment> {
    this.logger.log(`Creating shipment for prescription ${command.prescriptionId}`);

    const shipment = new Shipment();
    shipment.id = uuidv4();
    shipment.prescriptionId = command.prescriptionId;
    shipment.patientId = command.patientId;
    shipment.pharmacyId = command.pharmacyId;
    shipment.medications = command.medications;
    shipment.address = command.address;
    shipment.shipmentStatus = ShipmentStatus.CREATED;
    shipment.version = 0;
    shipment.createdAt = new Date();
    shipment.updatedAt = new Date();

    const saved = await this.shipmentRepository.save(shipment);

    if (command.messageId) {
      await this.shipmentRepository.markMessageProcessed(command.messageId, saved.id);
    }

    await this.eventPublisher.publishShipmentCreated(saved, command.correlationId);
    await this.eventPublisher.publishAuditEvent(
      saved.id,
      'ShipmentCreated',
      '',
      ShipmentStatus.CREATED,
      saved.prescriptionId,
      command.correlationId,
    );

    this.logger.log(`Shipment created: ${saved.id}`);
    return saved;
  }
}
