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
var CreateShipmentHandler_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.CreateShipmentHandler = void 0;
const cqrs_1 = require("@nestjs/cqrs");
const common_1 = require("@nestjs/common");
const uuid_1 = require("uuid");
const create_shipment_command_1 = require("./create-shipment.command");
const shipment_repository_1 = require("../../infrastructure/database/shipment.repository");
const shipment_event_publisher_1 = require("../../messaging/publishers/shipment-event.publisher");
const shipment_entity_1 = require("../../domain/shipment.entity");
const shipment_status_enum_1 = require("../../domain/shipment-status.enum");
let CreateShipmentHandler = CreateShipmentHandler_1 = class CreateShipmentHandler {
    constructor(shipmentRepository, eventPublisher) {
        this.shipmentRepository = shipmentRepository;
        this.eventPublisher = eventPublisher;
        this.logger = new common_1.Logger(CreateShipmentHandler_1.name);
    }
    async execute(command) {
        this.logger.log(`Creating shipment for prescription ${command.prescriptionId}`);
        const shipment = new shipment_entity_1.Shipment();
        shipment.id = (0, uuid_1.v4)();
        shipment.prescriptionId = command.prescriptionId;
        shipment.patientId = command.patientId;
        shipment.pharmacyId = command.pharmacyId;
        shipment.medications = command.medications;
        shipment.address = command.address;
        shipment.shipmentStatus = shipment_status_enum_1.ShipmentStatus.CREATED;
        shipment.version = 0;
        shipment.createdAt = new Date();
        shipment.updatedAt = new Date();
        const saved = await this.shipmentRepository.save(shipment);
        if (command.messageId) {
            await this.shipmentRepository.markMessageProcessed(command.messageId, saved.id);
        }
        await this.eventPublisher.publishShipmentCreated(saved, command.correlationId);
        await this.eventPublisher.publishAuditEvent(saved.id, 'ShipmentCreated', '', shipment_status_enum_1.ShipmentStatus.CREATED, saved.prescriptionId, command.correlationId);
        this.logger.log(`Shipment created: ${saved.id}`);
        return saved;
    }
};
exports.CreateShipmentHandler = CreateShipmentHandler;
exports.CreateShipmentHandler = CreateShipmentHandler = CreateShipmentHandler_1 = __decorate([
    (0, cqrs_1.CommandHandler)(create_shipment_command_1.CreateShipmentCommand),
    __metadata("design:paramtypes", [shipment_repository_1.ShipmentRepository,
        shipment_event_publisher_1.ShipmentEventPublisher])
], CreateShipmentHandler);
//# sourceMappingURL=create-shipment.handler.js.map