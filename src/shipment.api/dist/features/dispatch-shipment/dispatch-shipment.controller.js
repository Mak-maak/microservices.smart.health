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
var DispatchShipmentController_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.DispatchShipmentController = void 0;
const common_1 = require("@nestjs/common");
const cqrs_1 = require("@nestjs/cqrs");
const dispatch_shipment_dto_1 = require("./dispatch-shipment.dto");
const dispatch_shipment_command_1 = require("./dispatch-shipment.command");
const uuid_1 = require("uuid");
let DispatchShipmentController = DispatchShipmentController_1 = class DispatchShipmentController {
    constructor(commandBus) {
        this.commandBus = commandBus;
        this.logger = new common_1.Logger(DispatchShipmentController_1.name);
    }
    async dispatch(dto, correlationId) {
        const corrId = correlationId || (0, uuid_1.v4)();
        this.logger.log(`Dispatching shipment ${dto.shipmentId} [${corrId}]`);
        const result = await this.commandBus.execute(new dispatch_shipment_command_1.DispatchShipmentCommand(dto.shipmentId, dto.trackingNumber, corrId));
        return result;
    }
};
exports.DispatchShipmentController = DispatchShipmentController;
__decorate([
    (0, common_1.Post)('dispatch'),
    __param(0, (0, common_1.Body)()),
    __param(1, (0, common_1.Headers)('x-correlation-id')),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [dispatch_shipment_dto_1.DispatchShipmentDto, String]),
    __metadata("design:returntype", Promise)
], DispatchShipmentController.prototype, "dispatch", null);
exports.DispatchShipmentController = DispatchShipmentController = DispatchShipmentController_1 = __decorate([
    (0, common_1.Controller)('api/shipments'),
    __metadata("design:paramtypes", [cqrs_1.CommandBus])
], DispatchShipmentController);
//# sourceMappingURL=dispatch-shipment.controller.js.map