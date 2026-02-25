export declare class ShipmentDeliveredEvent {
    readonly shipmentId: string;
    readonly prescriptionId: string;
    readonly correlationId: string;
    constructor(shipmentId: string, prescriptionId: string, correlationId: string);
}
