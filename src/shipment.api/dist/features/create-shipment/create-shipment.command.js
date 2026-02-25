"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateShipmentCommand = void 0;
class CreateShipmentCommand {
    constructor(prescriptionId, patientId, pharmacyId, medications, address, correlationId, messageId) {
        this.prescriptionId = prescriptionId;
        this.patientId = patientId;
        this.pharmacyId = pharmacyId;
        this.medications = medications;
        this.address = address;
        this.correlationId = correlationId;
        this.messageId = messageId;
    }
}
exports.CreateShipmentCommand = CreateShipmentCommand;
//# sourceMappingURL=create-shipment.command.js.map