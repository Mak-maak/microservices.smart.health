import { ICommandHandler } from '@nestjs/cqrs';
import { MarkShipmentDeliveredCommand } from './mark-delivered.command';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { ShipmentEventPublisher } from '../../messaging/publishers/shipment-event.publisher';
import { Shipment } from '../../domain/shipment.entity';
export declare class MarkDeliveredHandler implements ICommandHandler<MarkShipmentDeliveredCommand> {
    private readonly shipmentRepository;
    private readonly eventPublisher;
    private readonly logger;
    constructor(shipmentRepository: ShipmentRepository, eventPublisher: ShipmentEventPublisher);
    execute(command: MarkShipmentDeliveredCommand): Promise<Shipment>;
}
