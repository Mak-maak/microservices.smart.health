import { CommandHandler, ICommandHandler } from '@nestjs/cqrs';
import { Logger, NotFoundException } from '@nestjs/common';
import { DispatchShipmentCommand } from './dispatch-shipment.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { ShipmentStatus } from '../../domain/shipment-status.enum';
import { Shipment } from '../../domain/shipment.entity';

@CommandHandler(DispatchShipmentCommand)
export class DispatchShipmentHandler implements ICommandHandler<DispatchShipmentCommand> {
  private readonly logger = new Logger(DispatchShipmentHandler.name);

  constructor(
    private readonly shipmentRepository: ShipmentRepository,
    private readonly eventPublisher: ShipmentEventPublisher,
  ) {}

  async execute(command: DispatchShipmentCommand): Promise<Shipment> {
    const shipment = await this.shipmentRepository.findById(command.shipmentId);
    if (!shipment) {
      throw new NotFoundException(`Shipment ${command.shipmentId} not found`);
    }

    if (shipment.shipmentStatus === ShipmentStatus.CREATED) {
      shipment.transitionTo(ShipmentStatus.PACKED);
      await this.shipmentRepository.save(shipment);
    }

    const previousStatus = shipment.shipmentStatus;
    shipment.transitionTo(ShipmentStatus.DISPATCHED);
    shipment.trackingNumber = command.trackingNumber;

    const saved = await this.shipmentRepository.save(shipment);

    await this.eventPublisher.publishShipmentDispatched(saved, command.correlationId);
    await this.eventPublisher.publishAuditEvent(
      saved.id,
      'ShipmentDispatched',
      previousStatus,
      ShipmentStatus.DISPATCHED,
      saved.prescriptionId,
      command.correlationId,
    );

    this.logger.log(`Shipment dispatched: ${saved.id}`);
    return saved;
  }
}
