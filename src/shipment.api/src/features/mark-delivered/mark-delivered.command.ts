export class MarkShipmentDeliveredCommand {
  constructor(
    public readonly shipmentId: string,
    public readonly correlationId: string,
  ) {}
}
