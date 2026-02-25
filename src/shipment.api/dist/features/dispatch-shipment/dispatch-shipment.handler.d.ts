import { ICommandHandler } from '@nestjs/cqrs';
import { DispatchShipmentCommand } from './dispatch-shipment.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { Shipment } from '../../domain/shipment.entity';
export declare class DispatchShipmentHandler implements ICommandHandler<DispatchShipmentCommand> {
    private readonly shipmentRepository;
    private readonly eventPublisher;
    private readonly logger;
    constructor(shipmentRepository: ShipmentRepository, eventPublisher: ShipmentEventPublisher);
    execute(command: DispatchShipmentCommand): Promise<Shipment>;
}
