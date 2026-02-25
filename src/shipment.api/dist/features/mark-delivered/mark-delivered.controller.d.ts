import { CommandBus } from '@nestjs/cqrs';
import { MarkDeliveredDto } from './mark-delivered.dto';
export declare class MarkDeliveredController {
    private readonly commandBus;
    private readonly logger;
    constructor(commandBus: CommandBus);
    deliver(dto: MarkDeliveredDto, correlationId?: string): Promise<any>;
}
