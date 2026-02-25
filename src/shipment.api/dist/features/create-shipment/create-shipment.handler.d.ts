import { ICommandHandler } from '@nestjs/cqrs';
import { CreateShipmentCommand } from './create-shipment.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { Shipment } from '../../domain/shipment.entity';
export declare class CreateShipmentHandler implements ICommandHandler<CreateShipmentCommand> {
    private readonly shipmentRepository;
    private readonly eventPublisher;
    private readonly logger;
    constructor(shipmentRepository: ShipmentRepository, eventPublisher: ShipmentEventPublisher);
    execute(command: CreateShipmentCommand): Promise<Shipment>;
}
