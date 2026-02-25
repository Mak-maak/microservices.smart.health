import { ICommandHandler } from '@nestjs/cqrs';
import { FailShipmentCommand } from './fail-shipment.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { Shipment } from '../../domain/shipment.entity';
export declare class FailShipmentHandler implements ICommandHandler<FailShipmentCommand> {
    private readonly shipmentRepository;
    private readonly eventPublisher;
    private readonly logger;
    constructor(shipmentRepository: ShipmentRepository, eventPublisher: ShipmentEventPublisher);
    execute(command: FailShipmentCommand): Promise<Shipment>;
}
