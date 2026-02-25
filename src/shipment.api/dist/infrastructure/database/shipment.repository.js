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
var ShipmentRepository_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.ShipmentRepository = void 0;
const common_1 = require("@nestjs/common");
const prisma_service_1 = require("./prisma.service");
const shipment_entity_1 = require("../../domain/shipment.entity");
let ShipmentRepository = ShipmentRepository_1 = class ShipmentRepository {
    constructor(prisma) {
        this.prisma = prisma;
        this.logger = new common_1.Logger(ShipmentRepository_1.name);
    }
    toDomain(record) {
        const shipment = new shipment_entity_1.Shipment();
        shipment.id = record.id;
        shipment.prescriptionId = record.prescriptionId;
        shipment.patientId = record.patientId;
        shipment.pharmacyId = record.pharmacyId;
        shipment.medications = record.medications;
        shipment.shipmentStatus = record.shipmentStatus;
        shipment.trackingNumber = record.trackingNumber;
        shipment.address = record.address;
        shipment.createdAt = record.createdAt;
        shipment.updatedAt = record.updatedAt;
        shipment.version = record.version;
        return shipment;
    }
    async save(shipment) {
        const record = await this.prisma.shipment.upsert({
            where: { id: shipment.id },
            create: {
                id: shipment.id,
                prescriptionId: shipment.prescriptionId,
                patientId: shipment.patientId,
                pharmacyId: shipment.pharmacyId,
                medications: shipment.medications,
                shipmentStatus: shipment.shipmentStatus,
                trackingNumber: shipment.trackingNumber,
                address: shipment.address,
                version: shipment.version,
            },
            update: {
                shipmentStatus: shipment.shipmentStatus,
                trackingNumber: shipment.trackingNumber,
                updatedAt: new Date(),
                version: shipment.version,
            },
        });
        return this.toDomain(record);
    }
    async findById(id) {
        const record = await this.prisma.shipment.findUnique({ where: { id } });
        return record ? this.toDomain(record) : null;
    }
    async findByPrescriptionId(prescriptionId) {
        const records = await this.prisma.shipment.findMany({ where: { prescriptionId } });
        return records.map((r) => this.toDomain(r));
    }
    async findByPatientId(patientId) {
        const records = await this.prisma.shipment.findMany({ where: { patientId } });
        return records.map((r) => this.toDomain(r));
    }
    async isMessageProcessed(messageId) {
        const record = await this.prisma.processedMessage.findUnique({ where: { id: messageId } });
        return !!record;
    }
    async markMessageProcessed(messageId, shipmentId) {
        await this.prisma.processedMessage.create({
            data: { id: messageId, shipmentId },
        });
    }
};
exports.ShipmentRepository = ShipmentRepository;
exports.ShipmentRepository = ShipmentRepository = ShipmentRepository_1 = __decorate([
    (0, common_1.Injectable)(),
    __metadata("design:paramtypes", [prisma_service_1.PrismaService])
], ShipmentRepository);
//# sourceMappingURL=shipment.repository.js.map