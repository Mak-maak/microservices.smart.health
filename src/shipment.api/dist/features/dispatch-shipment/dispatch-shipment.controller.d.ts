import { CommandBus } from '@nestjs/cqrs';
import { DispatchShipmentDto } from './dispatch-shipment.dto';
export declare class DispatchShipmentController {
    private readonly commandBus;
    private readonly logger;
    constructor(commandBus: CommandBus);
    dispatch(dto: DispatchShipmentDto, correlationId?: string): Promise<any>;
}
