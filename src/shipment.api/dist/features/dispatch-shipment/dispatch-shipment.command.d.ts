export declare class DispatchShipmentCommand {
    readonly shipmentId: string;
    readonly trackingNumber: string;
    readonly correlationId: string;
    constructor(shipmentId: string, trackingNumber: string, correlationId: string);
}
