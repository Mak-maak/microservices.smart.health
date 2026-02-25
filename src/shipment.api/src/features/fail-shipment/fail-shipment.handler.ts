import { CommandHandler, ICommandHandler } from '@nestjs/cqrs';
import { Logger, NotFoundException } from '@nestjs/common';
import { FailShipmentCommand } from './fail-shipment.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { ShipmentStatus } from '../../domain/shipment-status.enum';
import { Shipment } from '../../domain/shipment.entity';

@CommandHandler(FailShipmentCommand)
export class FailShipmentHandler implements ICommandHandler<FailShipmentCommand> {
  private readonly logger = new Logger(FailShipmentHandler.name);

  constructor(
    private readonly shipmentRepository: ShipmentRepository,
    private readonly eventPublisher: ShipmentEventPublisher,
  ) {}

  async execute(command: FailShipmentCommand): Promise<Shipment> {
    const shipment = await this.shipmentRepository.findById(command.shipmentId);
    if (!shipment) {
      throw new NotFoundException(`Shipment ${command.shipmentId} not found`);
    }

    const previousStatus = shipment.shipmentStatus;
    shipment.transitionTo(ShipmentStatus.FAILED);

    const saved = await this.shipmentRepository.save(shipment);

    await this.eventPublisher.publishShipmentFailed(saved, command.reason, command.correlationId);
    await this.eventPublisher.publishAuditEvent(
      saved.id,
      'ShipmentFailed',
      previousStatus,
      ShipmentStatus.FAILED,
      saved.prescriptionId,
      command.correlationId,
    );

    this.logger.log(`Shipment failed: ${saved.id}`);
    return saved;
  }
}
