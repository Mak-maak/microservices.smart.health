export class DispatchShipmentCommand {
  constructor(
    public readonly shipmentId: string,
    public readonly trackingNumber: string,
    public readonly correlationId: string,
  ) {}
}
