import { Controller, Post, Body, Headers, Logger } from '@nestjs/common';
import { CommandBus } from '@nestjs/cqrs';
import { DispatchShipmentDto } from './dispatch-shipment.dto';
import { DispatchShipmentCommand } from './dispatch-shipment.command';
import { v4 as uuidv4 } from 'uuid';

@Controller('api/shipments')
export class DispatchShipmentController {
  private readonly logger = new Logger(DispatchShipmentController.name);

  constructor(private readonly commandBus: CommandBus) {}

  @Post('dispatch')
  async dispatch(
    @Body() dto: DispatchShipmentDto,
    @Headers('x-correlation-id') correlationId?: string,
  ) {
    const corrId = correlationId || uuidv4();
    this.logger.log(`Dispatching shipment ${dto.shipmentId} [${corrId}]`);
    const result = await this.commandBus.execute(
      new DispatchShipmentCommand(dto.shipmentId, dto.trackingNumber, corrId),
    );
    return result;
  }
}
