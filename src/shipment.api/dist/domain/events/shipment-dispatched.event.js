"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ShipmentDispatchedEvent = void 0;
class ShipmentDispatchedEvent {
    constructor(shipmentId, prescriptionId, trackingNumber, correlationId) {
        this.shipmentId = shipmentId;
        this.prescriptionId = prescriptionId;
        this.trackingNumber = trackingNumber;
        this.correlationId = correlationId;
    }
}
exports.ShipmentDispatchedEvent = ShipmentDispatchedEvent;
//# sourceMappingURL=shipment-dispatched.event.js.map