import { MedicationItem, Address } from '../../domain/shipment.entity';

export class CreateShipmentCommand {
  constructor(
    public readonly prescriptionId: string,
    public readonly patientId: string,
    public readonly pharmacyId: string,
    public readonly medications: MedicationItem[],
    public readonly address: Address,
    public readonly correlationId: string,
    public readonly messageId?: string,
  ) {}
}
