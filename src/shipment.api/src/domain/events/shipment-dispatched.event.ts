export class ShipmentDispatchedEvent {
  constructor(
    public readonly shipmentId: string,
    public readonly prescriptionId: string,
    public readonly trackingNumber: string,
    public readonly correlationId: string,
  ) {}
}
