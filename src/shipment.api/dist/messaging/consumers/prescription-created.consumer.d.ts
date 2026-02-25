import { OnModuleInit } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { CommandBus } from '@nestjs/cqrs';
import { ServiceBusService } from '../../infrastructure/messaging/service-bus.service';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
export declare class PrescriptionCreatedConsumer implements OnModuleInit {
    private readonly serviceBusService;
    private readonly configService;
    private readonly commandBus;
    private readonly shipmentRepository;
    private readonly logger;
    private receiver;
    constructor(serviceBusService: ServiceBusService, configService: ConfigService, commandBus: CommandBus, shipmentRepository: ShipmentRepository);
    onModuleInit(): void;
    private handleMessage;
    private handleError;
}
