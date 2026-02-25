import { CommandHandler, ICommandHandler } from '@nestjs/cqrs';
import { Logger, NotFoundException } from '@nestjs/common';
import { MarkShipmentDeliveredCommand } from './mark-delivered.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { ShipmentStatus } from '../../domain/shipment-status.enum';
import { Shipment } from '../../domain/shipment.entity';

@CommandHandler(MarkShipmentDeliveredCommand)
export class MarkDeliveredHandler implements ICommandHandler<MarkShipmentDeliveredCommand> {
  private readonly logger = new Logger(MarkDeliveredHandler.name);

  constructor(
    private readonly shipmentRepository: ShipmentRepository,
    private readonly eventPublisher: ShipmentEventPublisher,
  ) {}

  async execute(command: MarkShipmentDeliveredCommand): Promise<Shipment> {
    const shipment = await this.shipmentRepository.findById(command.shipmentId);
    if (!shipment) {
      throw new NotFoundException(`Shipment ${command.shipmentId} not found`);
    }

    const previousStatus = shipment.shipmentStatus;
    shipment.transitionTo(ShipmentStatus.DELIVERED);

    const saved = await this.shipmentRepository.save(shipment);

    await this.eventPublisher.publishShipmentDelivered(saved, command.correlationId);
    await this.eventPublisher.publishAuditEvent(
      saved.id,
      'ShipmentDelivered',
      previousStatus,
      ShipmentStatus.DELIVERED,
      saved.prescriptionId,
      command.correlationId,
    );

    this.logger.log(`Shipment delivered: ${saved.id}`);
    return saved;
  }
}
