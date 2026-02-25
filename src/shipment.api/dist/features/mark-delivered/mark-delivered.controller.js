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
var MarkDeliveredController_1;
Object.defineProperty(exports, "__esModule", { value: true });
exports.MarkDeliveredController = void 0;
const common_1 = require("@nestjs/common");
const cqrs_1 = require("@nestjs/cqrs");
const mark_delivered_dto_1 = require("./mark-delivered.dto");
const mark_delivered_command_1 = require("./mark-delivered.command");
const uuid_1 = require("uuid");
let MarkDeliveredController = MarkDeliveredController_1 = class MarkDeliveredController {
    constructor(commandBus) {
        this.commandBus = commandBus;
        this.logger = new common_1.Logger(MarkDeliveredController_1.name);
    }
    async deliver(dto, correlationId) {
        const corrId = correlationId || (0, uuid_1.v4)();
        this.logger.log(`Marking shipment delivered ${dto.shipmentId} [${corrId}]`);
        const result = await this.commandBus.execute(new mark_delivered_command_1.MarkShipmentDeliveredCommand(dto.shipmentId, corrId));
        return result;
    }
};
exports.MarkDeliveredController = MarkDeliveredController;
__decorate([
    (0, common_1.Post)('deliver'),
    __param(0, (0, common_1.Body)()),
    __param(1, (0, common_1.Headers)('x-correlation-id')),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [mark_delivered_dto_1.MarkDeliveredDto, String]),
    __metadata("design:returntype", Promise)
], MarkDeliveredController.prototype, "deliver", null);
exports.MarkDeliveredController = MarkDeliveredController = MarkDeliveredController_1 = __decorate([
    (0, common_1.Controller)('api/shipments'),
    __metadata("design:paramtypes", [cqrs_1.CommandBus])
], MarkDeliveredController);
//# sourceMappingURL=mark-delivered.controller.js.map