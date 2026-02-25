"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.AppModule = void 0;
const common_1 = require("@nestjs/common");
const config_1 = require("@nestjs/config");
const cqrs_1 = require("@nestjs/cqrs");
const configuration_1 = require("./config/configuration");
const prisma_service_1 = require("./infrastructure/database/prisma.service");
const shipment_repository_1 = require("./infrastructure/database/shipment.repository");
const service_bus_module_1 = require("./infrastructure/messaging/service-bus.module");
const shipment_event_publisher_1 = require("./messaging/publishers/shipment-event.publisher");
const prescription_created_consumer_1 = require("./messaging/consumers/prescription-created.consumer");
const medicines_prescribed_consumer_1 = require("./messaging/consumers/medicines-prescribed.consumer");
const create_shipment_handler_1 = require("./features/create-shipment/create-shipment.handler");
const dispatch_shipment_handler_1 = require("./features/dispatch-shipment/dispatch-shipment.handler");
const dispatch_shipment_controller_1 = require("./features/dispatch-shipment/dispatch-shipment.controller");
const mark_delivered_handler_1 = require("./features/mark-delivered/mark-delivered.handler");
const mark_delivered_controller_1 = require("./features/mark-delivered/mark-delivered.controller");
const fail_shipment_handler_1 = require("./features/fail-shipment/fail-shipment.handler");
const get_shipment_handler_1 = require("./features/get-shipment/get-shipment.handler");
const get_shipment_controller_1 = require("./features/get-shipment/get-shipment.controller");
const correlation_id_middleware_1 = require("./common/correlation-id.middleware");
const CommandHandlers = [
    create_shipment_handler_1.CreateShipmentHandler,
    dispatch_shipment_handler_1.DispatchShipmentHandler,
    mark_delivered_handler_1.MarkDeliveredHandler,
    fail_shipment_handler_1.FailShipmentHandler,
];
const QueryHandlers = [
    get_shipment_handler_1.GetShipmentByIdHandler,
    get_shipment_handler_1.GetShipmentsByPrescriptionHandler,
    get_shipment_handler_1.GetShipmentsByPatientHandler,
];
let AppModule = class AppModule {
    configure(consumer) {
        consumer.apply(correlation_id_middleware_1.CorrelationIdMiddleware).forRoutes('*');
    }
};
exports.AppModule = AppModule;
exports.AppModule = AppModule = __decorate([
    (0, common_1.Module)({
        imports: [
            config_1.ConfigModule.forRoot({
                isGlobal: true,
                load: [configuration_1.configuration],
            }),
            cqrs_1.CqrsModule,
            service_bus_module_1.ServiceBusModule,
        ],
        controllers: [
            dispatch_shipment_controller_1.DispatchShipmentController,
            mark_delivered_controller_1.MarkDeliveredController,
            get_shipment_controller_1.GetShipmentController,
        ],
        providers: [
            prisma_service_1.PrismaService,
            shipment_repository_1.ShipmentRepository,
            shipment_event_publisher_1.ShipmentEventPublisher,
            prescription_created_consumer_1.PrescriptionCreatedConsumer,
            medicines_prescribed_consumer_1.MedicinesPrescribedConsumer,
            ...CommandHandlers,
            ...QueryHandlers,
        ],
    })
], AppModule);
//# sourceMappingURL=app.module.js.map