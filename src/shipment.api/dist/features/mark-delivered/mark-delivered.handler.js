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
var MarkDeliveredHandler_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.MarkDeliveredHandler = void 0;
const cqrs_1 = require("@nestjs/cqrs");
const common_1 = require("@nestjs/common");
const mark_delivered_command_1 = require("./mark-delivered.command");
const shipment_repository_1 = require("../../infrastructure/database/shipment.repository");
const shipment_event_publisher_1 = require("../../messaging/publishers/shipment-event.publisher");
const shipment_status_enum_1 = require("../../domain/shipment-status.enum");
let MarkDeliveredHandler = MarkDeliveredHandler_1 = class MarkDeliveredHandler {
    constructor(shipmentRepository, eventPublisher) {
        this.shipmentRepository = shipmentRepository;
        this.eventPublisher = eventPublisher;
        this.logger = new common_1.Logger(MarkDeliveredHandler_1.name);
    }
    async execute(command) {
        const shipment = await this.shipmentRepository.findById(command.shipmentId);
        if (!shipment) {
            throw new common_1.NotFoundException(`Shipment ${command.shipmentId} not found`);
        }
        const previousStatus = shipment.shipmentStatus;
        shipment.transitionTo(shipment_status_enum_1.ShipmentStatus.DELIVERED);
        const saved = await this.shipmentRepository.save(shipment);
        await this.eventPublisher.publishShipmentDelivered(saved, command.correlationId);
        await this.eventPublisher.publishAuditEvent(saved.id, 'ShipmentDelivered', previousStatus, shipment_status_enum_1.ShipmentStatus.DELIVERED, saved.prescriptionId, command.correlationId);
        this.logger.log(`Shipment delivered: ${saved.id}`);
        return saved;
    }
};
exports.MarkDeliveredHandler = MarkDeliveredHandler;
exports.MarkDeliveredHandler = MarkDeliveredHandler = MarkDeliveredHandler_1 = __decorate([
    (0, cqrs_1.CommandHandler)(mark_delivered_command_1.MarkShipmentDeliveredCommand),
    __metadata("design:paramtypes", [shipment_repository_1.ShipmentRepository,
        shipment_event_publisher_1.ShipmentEventPublisher])
], MarkDeliveredHandler);
//# sourceMappingURL=mark-delivered.handler.js.map