export declare class ShipmentCreatedEvent {
    readonly shipmentId: string;
    readonly prescriptionId: string;
    readonly patientId: string;
    readonly pharmacyId: string;
    readonly correlationId: string;
    constructor(shipmentId: string, prescriptionId: string, patientId: string, pharmacyId: string, correlationId: string);
}
