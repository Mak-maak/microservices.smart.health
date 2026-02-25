import { IsUUID } from 'class-validator';

export class MarkDeliveredDto {
  @IsUUID()
  shipmentId: string;
}
