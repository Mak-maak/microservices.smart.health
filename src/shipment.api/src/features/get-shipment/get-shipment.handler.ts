import { IQueryHandler, QueryHandler } from '@nestjs/cqrs';
import { Logger, NotFoundException } from '@nestjs/common';
import { GetShipmentByIdQuery } from './get-shipment-by-id.query';
import { GetShipmentsByPrescriptionQuery } from './get-shipments-by-prescription.query';
import { GetShipmentsByPatientQuery } from './get-shipments-by-patient.query';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { Shipment } from '../../domain/shipment.entity';

@QueryHandler(GetShipmentByIdQuery)
export class GetShipmentByIdHandler implements IQueryHandler<GetShipmentByIdQuery> {
  private readonly logger = new Logger(GetShipmentByIdHandler.name);

  constructor(private readonly shipmentRepository: ShipmentRepository) {}

  async execute(query: GetShipmentByIdQuery): Promise<Shipment> {
    const shipment = await this.shipmentRepository.findById(query.id);
    if (!shipment) {
      throw new NotFoundException(`Shipment ${query.id} not found`);
    }
    return shipment;
  }
}

@QueryHandler(GetShipmentsByPrescriptionQuery)
export class GetShipmentsByPrescriptionHandler implements IQueryHandler<GetShipmentsByPrescriptionQuery> {
  constructor(private readonly shipmentRepository: ShipmentRepository) {}

  async execute(query: GetShipmentsByPrescriptionQuery): Promise<Shipment[]> {
    return this.shipmentRepository.findByPrescriptionId(query.prescriptionId);
  }
}

@QueryHandler(GetShipmentsByPatientQuery)
export class GetShipmentsByPatientHandler implements IQueryHandler<GetShipmentsByPatientQuery> {
  constructor(private readonly shipmentRepository: ShipmentRepository) {}

  async execute(query: GetShipmentsByPatientQuery): Promise<Shipment[]> {
    return this.shipmentRepository.findByPatientId(query.patientId);
  }
}
