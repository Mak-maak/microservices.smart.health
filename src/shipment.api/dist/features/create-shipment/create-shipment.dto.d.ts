export declare class MedicationItemDto {
    medicineId: string;
    name: string;
    quantity: number;
    dosage?: string;
}
export declare class AddressDto {
    street: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
}
export declare class CreateShipmentDto {
    prescriptionId: string;
    patientId: string;
    pharmacyId: string;
    medications: MedicationItemDto[];
    address: AddressDto;
}
