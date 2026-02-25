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
var ServiceBusService_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.ServiceBusService = void 0;
const common_1 = require("@nestjs/common");
const config_1 = require("@nestjs/config");
const service_bus_1 = require("@azure/service-bus");
const uuid_1 = require("uuid");
let ServiceBusService = ServiceBusService_1 = class ServiceBusService {
    constructor(configService) {
        this.configService = configService;
        this.logger = new common_1.Logger(ServiceBusService_1.name);
        this.senders = new Map();
        this.connectionString = this.configService.get('serviceBus.connectionString');
    }
    onModuleInit() {
        if (this.connectionString) {
            this.client = new service_bus_1.ServiceBusClient(this.connectionString);
            this.logger.log('Service Bus client initialized');
        }
        else {
            this.logger.warn('Service Bus connection string not configured - messaging disabled');
        }
    }
    async onModuleDestroy() {
        for (const sender of this.senders.values()) {
            await sender.close();
        }
        if (this.client) {
            await this.client.close();
        }
    }
    async publishEvent(topicName, eventType, payload, correlationId) {
        if (!this.client) {
            this.logger.warn(`Service Bus not configured. Skipping publish to ${topicName}`);
            return;
        }
        try {
            if (!this.senders.has(topicName)) {
                this.senders.set(topicName, this.client.createSender(topicName));
            }
            const sender = this.senders.get(topicName);
            const message = {
                messageId: (0, uuid_1.v4)(),
                correlationId: correlationId,
                subject: eventType,
                body: payload,
                contentType: 'application/json',
                applicationProperties: {
                    eventType,
                    sourceService: 'shipment-service',
                    occurredAt: new Date().toISOString(),
                },
            };
            await sender.sendMessages(message);
            this.logger.log(`Published ${eventType} to ${topicName}`);
        }
        catch (error) {
            this.logger.error(`Failed to publish ${eventType} to ${topicName}: ${error.message}`);
            throw error;
        }
    }
    getClient() {
        return this.client || null;
    }
};
exports.ServiceBusService = ServiceBusService;
exports.ServiceBusService = ServiceBusService = ServiceBusService_1 = __decorate([
    (0, common_1.Injectable)(),
    __metadata("design:paramtypes", [config_1.ConfigService])
], ServiceBusService);
//# sourceMappingURL=service-bus.service.js.map