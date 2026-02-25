"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Shipment = exports.DomainException = void 0;
const shipment_status_enum_1 = require("./shipment-status.enum");
class DomainException extends Error {
    constructor(message) {
        super(message);
        this.name = 'DomainException';
    }
}
exports.DomainException = DomainException;
class Shipment {
    canTransitionTo(newStatus) {
        return Shipment.validTransitions[this.shipmentStatus]?.includes(newStatus) ?? false;
    }
    transitionTo(newStatus) {
        if (!this.canTransitionTo(newStatus)) {
            throw new DomainException(`Invalid state transition from ${this.shipmentStatus} to ${newStatus}`);
        }
        const previousStatus = this.shipmentStatus;
        this.shipmentStatus = newStatus;
        this.version += 1;
        return previousStatus;
    }
}
exports.Shipment = Shipment;
Shipment.validTransitions = {
    [shipment_status_enum_1.ShipmentStatus.CREATED]: [shipment_status_enum_1.ShipmentStatus.PACKED, shipment_status_enum_1.ShipmentStatus.FAILED],
    [shipment_status_enum_1.ShipmentStatus.PACKED]: [shipment_status_enum_1.ShipmentStatus.DISPATCHED, shipment_status_enum_1.ShipmentStatus.FAILED],
    [shipment_status_enum_1.ShipmentStatus.DISPATCHED]: [shipment_status_enum_1.ShipmentStatus.DELIVERED, shipment_status_enum_1.ShipmentStatus.FAILED],
    [shipment_status_enum_1.ShipmentStatus.DELIVERED]: [],
    [shipment_status_enum_1.ShipmentStatus.FAILED]: [],
};
//# sourceMappingURL=shipment.entity.js.map