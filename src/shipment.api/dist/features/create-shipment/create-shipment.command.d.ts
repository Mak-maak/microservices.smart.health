import { MedicationItem, Address } from '../../domain/shipment.entity';
export declare class CreateShipmentCommand {
    readonly prescriptionId: string;
    readonly patientId: string;
    readonly pharmacyId: string;
    readonly medications: MedicationItem[];
    readonly address: Address;
    readonly correlationId: string;
    readonly messageId?: string;
    constructor(prescriptionId: string, patientId: string, pharmacyId: string, medications: MedicationItem[], address: Address, correlationId: string, messageId?: string);
}
