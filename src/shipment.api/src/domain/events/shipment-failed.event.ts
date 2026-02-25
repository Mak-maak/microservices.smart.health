export class ShipmentFailedEvent {
  constructor(
    public readonly shipmentId: string,
    public readonly prescriptionId: string,
    public readonly reason: string,
    public readonly correlationId: string,
  ) {}
}
