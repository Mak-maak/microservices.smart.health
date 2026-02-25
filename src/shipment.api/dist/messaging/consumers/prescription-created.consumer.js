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
var PrescriptionCreatedConsumer_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrescriptionCreatedConsumer = void 0;
const common_1 = require("@nestjs/common");
const config_1 = require("@nestjs/config");
const cqrs_1 = require("@nestjs/cqrs");
const service_bus_service_1 = require("../../infrastructure/messaging/service-bus.service");
const shipment_repository_1 = require("../../infrastructure/database/shipment.repository");
const create_shipment_command_1 = require("../../features/create-shipment/create-shipment.command");
const uuid_1 = require("uuid");
let PrescriptionCreatedConsumer = PrescriptionCreatedConsumer_1 = class PrescriptionCreatedConsumer {
    constructor(serviceBusService, configService, commandBus, shipmentRepository) {
        this.serviceBusService = serviceBusService;
        this.configService = configService;
        this.commandBus = commandBus;
        this.shipmentRepository = shipmentRepository;
        this.logger = new common_1.Logger(PrescriptionCreatedConsumer_1.name);
    }
    onModuleInit() {
        const client = this.serviceBusService.getClient();
        if (!client) {
            this.logger.warn('Service Bus not available - PrescriptionCreated consumer disabled');
            return;
        }
        const topicName = this.configService.get('serviceBus.topics.prescriptionCreated');
        const subscriptionName = this.configService.get('serviceBus.subscriptionName');
        this.receiver = client.createReceiver(topicName, subscriptionName);
        this.receiver.subscribe({
            processMessage: this.handleMessage.bind(this),
            processError: this.handleError.bind(this),
        });
        this.logger.log(`Subscribed to ${topicName}/${subscriptionName}`);
    }
    async handleMessage(message) {
        const messageId = message.messageId;
        const correlationId = message.correlationId || (0, uuid_1.v4)();
        this.logger.log(`Received PrescriptionCreated message: ${messageId}`);
        const alreadyProcessed = await this.shipmentRepository.isMessageProcessed(messageId);
        if (alreadyProcessed) {
            this.logger.log(`Message ${messageId} already processed - skipping`);
            await this.receiver.completeMessage(message);
            return;
        }
        const body = message.body;
        try {
            const command = new create_shipment_command_1.CreateShipmentCommand(body.prescriptionId, body.patientId, body.pharmacyId || (0, uuid_1.v4)(), body.medications || [], body.address || { street: '', city: '', state: '', postalCode: '', country: '' }, correlationId, messageId);
            await this.commandBus.execute(command);
            await this.receiver.completeMessage(message);
            this.logger.log(`PrescriptionCreated processed successfully: ${messageId}`);
        }
        catch (error) {
            this.logger.error(`Failed to process PrescriptionCreated: ${error.message}`);
            await this.receiver.deadLetterMessage(message, {
                deadLetterReason: 'ProcessingFailed',
                deadLetterErrorDescription: error.message,
            });
        }
    }
    async handleError(args) {
        this.logger.error(`Service Bus error: ${args.error.message}`);
    }
};
exports.PrescriptionCreatedConsumer = PrescriptionCreatedConsumer;
exports.PrescriptionCreatedConsumer = PrescriptionCreatedConsumer = PrescriptionCreatedConsumer_1 = __decorate([
    (0, common_1.Injectable)(),
    __metadata("design:paramtypes", [service_bus_service_1.ServiceBusService,
        config_1.ConfigService,
        cqrs_1.CommandBus,
        shipment_repository_1.ShipmentRepository])
], PrescriptionCreatedConsumer);
//# sourceMappingURL=prescription-created.consumer.js.map