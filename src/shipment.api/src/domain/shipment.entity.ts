import { ShipmentStatus } from './shipment-status.enum';

export class DomainException extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'DomainException';
  }
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

export class Shipment {
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

  static validTransitions: Record<ShipmentStatus, ShipmentStatus[]> = {
    [ShipmentStatus.CREATED]: [ShipmentStatus.PACKED, ShipmentStatus.FAILED],
    [ShipmentStatus.PACKED]: [ShipmentStatus.DISPATCHED, ShipmentStatus.FAILED],
    [ShipmentStatus.DISPATCHED]: [ShipmentStatus.DELIVERED, ShipmentStatus.FAILED],
    [ShipmentStatus.DELIVERED]: [],
    [ShipmentStatus.FAILED]: [],
  };

  canTransitionTo(newStatus: ShipmentStatus): boolean {
    return Shipment.validTransitions[this.shipmentStatus]?.includes(newStatus) ?? false;
  }

  transitionTo(newStatus: ShipmentStatus): ShipmentStatus {
    if (!this.canTransitionTo(newStatus)) {
      throw new DomainException(
        `Invalid state transition from ${this.shipmentStatus} to ${newStatus}`,
      );
    }
    const previousStatus = this.shipmentStatus;
    this.shipmentStatus = newStatus;
    this.version += 1;
    return previousStatus;
  }
}
