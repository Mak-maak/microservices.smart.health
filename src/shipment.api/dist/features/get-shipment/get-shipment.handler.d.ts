import { IQueryHandler } from '@nestjs/cqrs';
import { GetShipmentByIdQuery } from './get-shipment-by-id.query';
import { GetShipmentsByPrescriptionQuery } from './get-shipments-by-prescription.query';
import { GetShipmentsByPatientQuery } from './get-shipments-by-patient.query';
import { ShipmentRepository } from '../../infrastructure/database/shipment.repository';
import { Shipment } from '../../domain/shipment.entity';
export declare class GetShipmentByIdHandler implements IQueryHandler<GetShipmentByIdQuery> {
    private readonly shipmentRepository;
    private readonly logger;
    constructor(shipmentRepository: ShipmentRepository);
    execute(query: GetShipmentByIdQuery): Promise<Shipment>;
}
export declare class GetShipmentsByPrescriptionHandler implements IQueryHandler<GetShipmentsByPrescriptionQuery> {
    private readonly shipmentRepository;
    constructor(shipmentRepository: ShipmentRepository);
    execute(query: GetShipmentsByPrescriptionQuery): Promise<Shipment[]>;
}
export declare class GetShipmentsByPatientHandler implements IQueryHandler<GetShipmentsByPatientQuery> {
    private readonly shipmentRepository;
    constructor(shipmentRepository: ShipmentRepository);
    execute(query: GetShipmentsByPatientQuery): Promise<Shipment[]>;
}
