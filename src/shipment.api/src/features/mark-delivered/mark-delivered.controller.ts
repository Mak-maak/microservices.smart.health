import { Controller, Post, Body, Headers, Logger } from '@nestjs/common';
import { CommandBus } from '@nestjs/cqrs';
import { MarkDeliveredDto } from './mark-delivered.dto';
import { MarkShipmentDeliveredCommand } from './mark-delivered.command';
import { v4 as uuidv4 } from 'uuid';

@Controller('api/shipments')
export class MarkDeliveredController {
  private readonly logger = new Logger(MarkDeliveredController.name);

  constructor(private readonly commandBus: CommandBus) {}

  @Post('deliver')
  async deliver(
    @Body() dto: MarkDeliveredDto,
    @Headers('x-correlation-id') correlationId?: string,
  ) {
    const corrId = correlationId || uuidv4();
    this.logger.log(`Marking shipment delivered ${dto.shipmentId} [${corrId}]`);
    const result = await this.commandBus.execute(
      new MarkShipmentDeliveredCommand(dto.shipmentId, corrId),
    );
    return result;
  }
}
