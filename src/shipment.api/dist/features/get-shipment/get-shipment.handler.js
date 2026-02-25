"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var GetShipmentByIdHandler_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetShipmentsByPatientHandler = exports.GetShipmentsByPrescriptionHandler = exports.GetShipmentByIdHandler = void 0;
const cqrs_1 = require("@nestjs/cqrs");
const common_1 = require("@nestjs/common");
const get_shipment_by_id_query_1 = require("./get-shipment-by-id.query");
const get_shipments_by_prescription_query_1 = require("./get-shipments-by-prescription.query");
const get_shipments_by_patient_query_1 = require("./get-shipments-by-patient.query");
const shipment_repository_1 = require("../../infrastructure/database/shipment.repository");
let GetShipmentByIdHandler = GetShipmentByIdHandler_1 = class GetShipmentByIdHandler {
    constructor(shipmentRepository) {
        this.shipmentRepository = shipmentRepository;
        this.logger = new common_1.Logger(GetShipmentByIdHandler_1.name);
    }
    async execute(query) {
        const shipment = await this.shipmentRepository.findById(query.id);
        if (!shipment) {
            throw new common_1.NotFoundException(`Shipment ${query.id} not found`);
        }
        return shipment;
    }
};
exports.GetShipmentByIdHandler = GetShipmentByIdHandler;
exports.GetShipmentByIdHandler = GetShipmentByIdHandler = GetShipmentByIdHandler_1 = __decorate([
    (0, cqrs_1.QueryHandler)(get_shipment_by_id_query_1.GetShipmentByIdQuery),
    __metadata("design:paramtypes", [shipment_repository_1.ShipmentRepository])
], GetShipmentByIdHandler);
let GetShipmentsByPrescriptionHandler = class GetShipmentsByPrescriptionHandler {
    constructor(shipmentRepository) {
        this.shipmentRepository = shipmentRepository;
    }
    async execute(query) {
        return this.shipmentRepository.findByPrescriptionId(query.prescriptionId);
    }
};
exports.GetShipmentsByPrescriptionHandler = GetShipmentsByPrescriptionHandler;
exports.GetShipmentsByPrescriptionHandler = GetShipmentsByPrescriptionHandler = __decorate([
    (0, cqrs_1.QueryHandler)(get_shipments_by_prescription_query_1.GetShipmentsByPrescriptionQuery),
    __metadata("design:paramtypes", [shipment_repository_1.ShipmentRepository])
], GetShipmentsByPrescriptionHandler);
let GetShipmentsByPatientHandler = class GetShipmentsByPatientHandler {
    constructor(shipmentRepository) {
        this.shipmentRepository = shipmentRepository;
    }
    async execute(query) {
        return this.shipmentRepository.findByPatientId(query.patientId);
    }
};
exports.GetShipmentsByPatientHandler = GetShipmentsByPatientHandler;
exports.GetShipmentsByPatientHandler = GetShipmentsByPatientHandler = __decorate([
    (0, cqrs_1.QueryHandler)(get_shipments_by_patient_query_1.GetShipmentsByPatientQuery),
    __metadata("design:paramtypes", [shipment_repository_1.ShipmentRepository])
], GetShipmentsByPatientHandler);
//# sourceMappingURL=get-shipment.handler.js.map