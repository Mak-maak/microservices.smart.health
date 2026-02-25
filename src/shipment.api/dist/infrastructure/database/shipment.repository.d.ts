import { PrismaService } from './prisma.service';
import { Shipment } from '../../domain/shipment.entity';
export declare class ShipmentRepository {
    private readonly prisma;
    private readonly logger;
    constructor(prisma: PrismaService);
    private toDomain;
    save(shipment: Shipment): Promise<Shipment>;
    findById(id: string): Promise<Shipment | null>;
    findByPrescriptionId(prescriptionId: string): Promise<Shipment[]>;
    findByPatientId(patientId: string): Promise<Shipment[]>;
    isMessageProcessed(messageId: string): Promise<boolean>;
    markMessageProcessed(messageId: string, shipmentId?: string): Promise<void>;
}
