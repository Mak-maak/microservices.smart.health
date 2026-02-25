export class ShipmentCreatedEvent {
  constructor(
    public readonly shipmentId: string,
    public readonly prescriptionId: string,
    public readonly patientId: string,
    public readonly pharmacyId: string,
    public readonly correlationId: string,
  ) {}
}
