export declare class FailShipmentCommand {
    readonly shipmentId: string;
    readonly reason: string;
    readonly correlationId: string;
    constructor(shipmentId: string, reason: string, correlationId: string);
}
