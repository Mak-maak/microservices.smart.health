export declare class ShipmentDispatchedEvent {
    readonly shipmentId: string;
    readonly prescriptionId: string;
    readonly trackingNumber: string;
    readonly correlationId: string;
    constructor(shipmentId: string, prescriptionId: string, trackingNumber: string, correlationId: string);
}
