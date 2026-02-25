"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ShipmentFailedEvent = void 0;
class ShipmentFailedEvent {
    constructor(shipmentId, prescriptionId, reason, correlationId) {
        this.shipmentId = shipmentId;
        this.prescriptionId = prescriptionId;
        this.reason = reason;
        this.correlationId = correlationId;
    }
}
exports.ShipmentFailedEvent = ShipmentFailedEvent;
//# sourceMappingURL=shipment-failed.event.js.map