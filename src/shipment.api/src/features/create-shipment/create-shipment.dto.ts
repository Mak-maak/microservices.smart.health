import { IsString, IsUUID, IsArray, IsObject, ValidateNested, IsOptional } from 'class-validator';
import { Type } from 'class-transformer';

export class MedicationItemDto {
  @IsUUID()
  medicineId: string;

  @IsString()
  name: string;

  quantity: number;

  @IsOptional()
  @IsString()
  dosage?: string;
}

export class AddressDto {
  @IsString()
  street: string;

  @IsString()
  city: string;

  @IsString()
  state: string;

  @IsString()
  postalCode: string;

  @IsString()
  country: string;
}

export class CreateShipmentDto {
  @IsUUID()
  prescriptionId: string;

  @IsUUID()
  patientId: string;

  @IsUUID()
  pharmacyId: string;

  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => MedicationItemDto)
  medications: MedicationItemDto[];

  @IsObject()
  @ValidateNested()
  @Type(() => AddressDto)
  address: AddressDto;
}
