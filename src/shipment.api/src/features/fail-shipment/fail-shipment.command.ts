export class FailShipmentCommand {
  constructor(
    public readonly shipmentId: string,
    public readonly reason: string,
    public readonly correlationId: string,
  ) {}
}
