import { ConfigService } from '@nestjs/config';
import { ServiceBusService } from '../../infrastructure/messaging/service-bus.service';
export declare class ShipmentEventPublisher {
    private readonly serviceBusService;
    private readonly configService;
    private readonly logger;
    constructor(serviceBusService: ServiceBusService, configService: ConfigService);
    publishShipmentCreated(shipment: any, correlationId: string): Promise<void>;
    publishShipmentDispatched(shipment: any, correlationId: string): Promise<void>;
    publishShipmentDelivered(shipment: any, correlationId: string): Promise<void>;
    publishShipmentFailed(shipment: any, reason: string, correlationId: string): Promise<void>;
    publishAuditEvent(aggregateId: string, eventType: string, previousStatus: string, newStatus: string, prescriptionId: string, correlationId: string): Promise<void>;
}
