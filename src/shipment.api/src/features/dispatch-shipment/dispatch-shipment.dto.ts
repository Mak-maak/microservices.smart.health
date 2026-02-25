import { IsString, IsUUID, IsNotEmpty } from 'class-validator';

export class DispatchShipmentDto {
  @IsUUID()
  shipmentId: string;

  @IsString()
  @IsNotEmpty()
  trackingNumber: string;
}
