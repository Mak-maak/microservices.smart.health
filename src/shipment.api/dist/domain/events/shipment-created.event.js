"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ShipmentCreatedEvent = void 0;
class ShipmentCreatedEvent {
    constructor(shipmentId, prescriptionId, patientId, pharmacyId, correlationId) {
        this.shipmentId = shipmentId;
        this.prescriptionId = prescriptionId;
        this.patientId = patientId;
        this.pharmacyId = pharmacyId;
        this.correlationId = correlationId;
    }
}
exports.ShipmentCreatedEvent = ShipmentCreatedEvent;
//# sourceMappingURL=shipment-created.event.js.map