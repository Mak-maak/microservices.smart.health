"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.FailShipmentCommand = void 0;
class FailShipmentCommand {
    constructor(shipmentId, reason, correlationId) {
        this.shipmentId = shipmentId;
        this.reason = reason;
        this.correlationId = correlationId;
    }
}
exports.FailShipmentCommand = FailShipmentCommand;
//# sourceMappingURL=fail-shipment.command.js.map