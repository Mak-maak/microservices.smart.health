export class ShipmentDeliveredEvent {
  constructor(
    public readonly shipmentId: string,
    public readonly prescriptionId: string,
    public readonly correlationId: string,
  ) {}
}
