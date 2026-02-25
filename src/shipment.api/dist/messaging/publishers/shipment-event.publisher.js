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
var ShipmentEventPublisher_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.ShipmentEventPublisher = void 0;
const common_1 = require("@nestjs/common");
const config_1 = require("@nestjs/config");
const service_bus_service_1 = require("../../infrastructure/messaging/service-bus.service");
const uuid_1 = require("uuid");
let ShipmentEventPublisher = ShipmentEventPublisher_1 = class ShipmentEventPublisher {
    constructor(serviceBusService, configService) {
        this.serviceBusService = serviceBusService;
        this.configService = configService;
        this.logger = new common_1.Logger(ShipmentEventPublisher_1.name);
    }
    async publishShipmentCreated(shipment, correlationId) {
        const topic = this.configService.get('serviceBus.topics.shipmentCreated');
        await this.serviceBusService.publishEvent(topic, 'ShipmentCreated', {
            shipmentId: shipment.id,
            prescriptionId: shipment.prescriptionId,
            patientId: shipment.patientId,
            pharmacyId: shipment.pharmacyId,
            shipmentStatus: shipment.shipmentStatus,
            correlationId,
        }, correlationId);
    }
    async publishShipmentDispatched(shipment, correlationId) {
        const topic = this.configService.get('serviceBus.topics.shipmentDispatched');
        await this.serviceBusService.publishEvent(topic, 'ShipmentDispatched', {
            shipmentId: shipment.id,
            prescriptionId: shipment.prescriptionId,
            trackingNumber: shipment.trackingNumber,
            shipmentStatus: shipment.shipmentStatus,
            correlationId,
        }, correlationId);
    }
    async publishShipmentDelivered(shipment, correlationId) {
        const topic = this.configService.get('serviceBus.topics.shipmentDelivered');
        await this.serviceBusService.publishEvent(topic, 'ShipmentDelivered', {
            shipmentId: shipment.id,
            prescriptionId: shipment.prescriptionId,
            shipmentStatus: shipment.shipmentStatus,
            correlationId,
        }, correlationId);
    }
    async publishShipmentFailed(shipment, reason, correlationId) {
        const topic = this.configService.get('serviceBus.topics.shipmentFailed');
        await this.serviceBusService.publishEvent(topic, 'ShipmentFailed', {
            shipmentId: shipment.id,
            prescriptionId: shipment.prescriptionId,
            reason,
            shipmentStatus: shipment.shipmentStatus,
            correlationId,
        }, correlationId);
    }
    async publishAuditEvent(aggregateId, eventType, previousStatus, newStatus, prescriptionId, correlationId) {
        const topic = this.configService.get('serviceBus.topics.auditEvents');
        const auditEvent = {
            eventId: (0, uuid_1.v4)(),
            aggregateId,
            eventType,
            occurredAt: new Date().toISOString(),
            sourceService: 'shipment-service',
            payload: {
                previousStatus,
                newStatus,
                shipmentId: aggregateId,
                prescriptionId,
                correlationId,
                timestamp: new Date().toISOString(),
            },
        };
        await this.serviceBusService.publishEvent(topic, 'AuditEvent', auditEvent, correlationId);
    }
};
exports.ShipmentEventPublisher = ShipmentEventPublisher;
exports.ShipmentEventPublisher = ShipmentEventPublisher = ShipmentEventPublisher_1 = __decorate([
    (0, common_1.Injectable)(),
    __metadata("design:paramtypes", [service_bus_service_1.ServiceBusService,
        config_1.ConfigService])
], ShipmentEventPublisher);
//# sourceMappingURL=shipment-event.publisher.js.map