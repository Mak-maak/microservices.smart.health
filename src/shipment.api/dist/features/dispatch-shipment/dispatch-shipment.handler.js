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
var DispatchShipmentHandler_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.DispatchShipmentHandler = void 0;
const cqrs_1 = require("@nestjs/cqrs");
const common_1 = require("@nestjs/common");
const dispatch_shipment_command_1 = require("./dispatch-shipment.command");
const shipment_repository_1 = require("../../infrastructure/database/shipment.repository");
const shipment_event_publisher_1 = require("../../messaging/publishers/shipment-event.publisher");
const shipment_status_enum_1 = require("../../domain/shipment-status.enum");
let DispatchShipmentHandler = DispatchShipmentHandler_1 = class DispatchShipmentHandler {
    constructor(shipmentRepository, eventPublisher) {
        this.shipmentRepository = shipmentRepository;
        this.eventPublisher = eventPublisher;
        this.logger = new common_1.Logger(DispatchShipmentHandler_1.name);
    }
    async execute(command) {
        const shipment = await this.shipmentRepository.findById(command.shipmentId);
        if (!shipment) {
            throw new common_1.NotFoundException(`Shipment ${command.shipmentId} not found`);
        }
        if (shipment.shipmentStatus === shipment_status_enum_1.ShipmentStatus.CREATED) {
            shipment.transitionTo(shipment_status_enum_1.ShipmentStatus.PACKED);
            await this.shipmentRepository.save(shipment);
        }
        const previousStatus = shipment.shipmentStatus;
        shipment.transitionTo(shipment_status_enum_1.ShipmentStatus.DISPATCHED);
        shipment.trackingNumber = command.trackingNumber;
        const saved = await this.shipmentRepository.save(shipment);
        await this.eventPublisher.publishShipmentDispatched(saved, command.correlationId);
        await this.eventPublisher.publishAuditEvent(saved.id, 'ShipmentDispatched', previousStatus, shipment_status_enum_1.ShipmentStatus.DISPATCHED, saved.prescriptionId, command.correlationId);
        this.logger.log(`Shipment dispatched: ${saved.id}`);
        return saved;
    }
};
exports.DispatchShipmentHandler = DispatchShipmentHandler;
exports.DispatchShipmentHandler = DispatchShipmentHandler = DispatchShipmentHandler_1 = __decorate([
    (0, cqrs_1.CommandHandler)(dispatch_shipment_command_1.DispatchShipmentCommand),
    __metadata("design:paramtypes", [shipment_repository_1.ShipmentRepository,
        shipment_event_publisher_1.ShipmentEventPublisher])
], DispatchShipmentHandler);
//# sourceMappingURL=dispatch-shipment.handler.js.map