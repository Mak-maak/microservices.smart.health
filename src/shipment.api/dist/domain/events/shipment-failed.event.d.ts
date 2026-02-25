export declare class ShipmentFailedEvent {
    readonly shipmentId: string;
    readonly prescriptionId: string;
    readonly reason: string;
    readonly correlationId: string;
    constructor(shipmentId: string, prescriptionId: string, reason: string, correlationId: string);
}
