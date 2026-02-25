import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { ServiceBusService } from '../../infrastructure/messaging/service-bus.service';
import { v4 as uuidv4 } from 'uuid';

@Injectable()
export class ShipmentEventPublisher {
  private readonly logger = new Logger(ShipmentEventPublisher.name);

  constructor(
    private readonly serviceBusService: ServiceBusService,
    private readonly configService: ConfigService,
  ) {}

  async publishShipmentCreated(shipment: any, correlationId: string): Promise<void> {
    const topic = this.configService.get<string>('serviceBus.topics.shipmentCreated');
    await this.serviceBusService.publishEvent(topic, 'ShipmentCreated', {
      shipmentId: shipment.id,
      prescriptionId: shipment.prescriptionId,
      patientId: shipment.patientId,
      pharmacyId: shipment.pharmacyId,
      shipmentStatus: shipment.shipmentStatus,
      correlationId,
    }, correlationId);
  }

  async publishShipmentDispatched(shipment: any, correlationId: string): Promise<void> {
    const topic = this.configService.get<string>('serviceBus.topics.shipmentDispatched');
    await this.serviceBusService.publishEvent(topic, 'ShipmentDispatched', {
      shipmentId: shipment.id,
      prescriptionId: shipment.prescriptionId,
      trackingNumber: shipment.trackingNumber,
      shipmentStatus: shipment.shipmentStatus,
      correlationId,
    }, correlationId);
  }

  async publishShipmentDelivered(shipment: any, correlationId: string): Promise<void> {
    const topic = this.configService.get<string>('serviceBus.topics.shipmentDelivered');
    await this.serviceBusService.publishEvent(topic, 'ShipmentDelivered', {
      shipmentId: shipment.id,
      prescriptionId: shipment.prescriptionId,
      shipmentStatus: shipment.shipmentStatus,
      correlationId,
    }, correlationId);
  }

  async publishShipmentFailed(shipment: any, reason: string, correlationId: string): Promise<void> {
    const topic = this.configService.get<string>('serviceBus.topics.shipmentFailed');
    await this.serviceBusService.publishEvent(topic, 'ShipmentFailed', {
      shipmentId: shipment.id,
      prescriptionId: shipment.prescriptionId,
      reason,
      shipmentStatus: shipment.shipmentStatus,
      correlationId,
    }, correlationId);
  }

  async publishAuditEvent(
    aggregateId: string,
    eventType: string,
    previousStatus: string,
    newStatus: string,
    prescriptionId: string,
    correlationId: string,
  ): Promise<void> {
    const topic = this.configService.get<string>('serviceBus.topics.auditEvents');
    const auditEvent = {
      eventId: uuidv4(),
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
}
