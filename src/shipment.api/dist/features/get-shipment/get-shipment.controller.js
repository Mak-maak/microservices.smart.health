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
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
var GetShipmentController_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetShipmentController = void 0;
const common_1 = require("@nestjs/common");
const cqrs_1 = require("@nestjs/cqrs");
const get_shipment_by_id_query_1 = require("./get-shipment-by-id.query");
const get_shipments_by_prescription_query_1 = require("./get-shipments-by-prescription.query");
const get_shipments_by_patient_query_1 = require("./get-shipments-by-patient.query");
let GetShipmentController = GetShipmentController_1 = class GetShipmentController {
    constructor(queryBus) {
        this.queryBus = queryBus;
        this.logger = new common_1.Logger(GetShipmentController_1.name);
    }
    async getByPrescription(prescriptionId) {
        return this.queryBus.execute(new get_shipments_by_prescription_query_1.GetShipmentsByPrescriptionQuery(prescriptionId));
    }
    async getByPatient(patientId) {
        return this.queryBus.execute(new get_shipments_by_patient_query_1.GetShipmentsByPatientQuery(patientId));
    }
    async getById(id) {
        return this.queryBus.execute(new get_shipment_by_id_query_1.GetShipmentByIdQuery(id));
    }
};
exports.GetShipmentController = GetShipmentController;
__decorate([
    (0, common_1.Get)('prescription/:prescriptionId'),
    __param(0, (0, common_1.Param)('prescriptionId')),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [String]),
    __metadata("design:returntype", Promise)
], GetShipmentController.prototype, "getByPrescription", null);
__decorate([
    (0, common_1.Get)('patient/:patientId'),
    __param(0, (0, common_1.Param)('patientId')),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [String]),
    __metadata("design:returntype", Promise)
], GetShipmentController.prototype, "getByPatient", null);
__decorate([
    (0, common_1.Get)(':id'),
    __param(0, (0, common_1.Param)('id')),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [String]),
    __metadata("design:returntype", Promise)
], GetShipmentController.prototype, "getById", null);
exports.GetShipmentController = GetShipmentController = GetShipmentController_1 = __decorate([
    (0, common_1.Controller)('api/shipments'),
    __metadata("design:paramtypes", [cqrs_1.QueryBus])
], GetShipmentController);
//# sourceMappingURL=get-shipment.controller.js.map