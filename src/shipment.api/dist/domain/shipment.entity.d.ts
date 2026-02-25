import { ShipmentStatus } from './shipment-status.enum';
export declare class DomainException extends Error {
    constructor(message: string);
}
export interface MedicationItem {
    medicineId: string;
    name: string;
    quantity: number;
    dosage?: string;
}
export interface Address {
    street: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
}
export declare class Shipment {
    id: string;
    prescriptionId: string;
    patientId: string;
    pharmacyId: string;
    medications: MedicationItem[];
    shipmentStatus: ShipmentStatus;
    trackingNumber?: string;
    address: Address;
    createdAt: Date;
    updatedAt: Date;
    version: number;
    static validTransitions: Record<ShipmentStatus, ShipmentStatus[]>;
    canTransitionTo(newStatus: ShipmentStatus): boolean;
    transitionTo(newStatus: ShipmentStatus): ShipmentStatus;
}
