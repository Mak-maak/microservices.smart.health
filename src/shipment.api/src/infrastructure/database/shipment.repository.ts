import { Injectable, Logger } from '@nestjs/common';
import { PrismaService } from './prisma.service';
import { Shipment } from '../../domain/shipment.entity';
import { ShipmentStatus } from '../../domain/shipment-status.enum';

@Injectable()
export class ShipmentRepository {
  private readonly logger = new Logger(ShipmentRepository.name);

  constructor(private readonly prisma: PrismaService) {}

  private toDomain(record: any): Shipment {
    const shipment = new Shipment();
    shipment.id = record.id;
    shipment.prescriptionId = record.prescriptionId;
    shipment.patientId = record.patientId;
    shipment.pharmacyId = record.pharmacyId;
    shipment.medications = record.medications as any;
    shipment.shipmentStatus = record.shipmentStatus as ShipmentStatus;
    shipment.trackingNumber = record.trackingNumber;
    shipment.address = record.address as any;
    shipment.createdAt = record.createdAt;
    shipment.updatedAt = record.updatedAt;
    shipment.version = record.version;
    return shipment;
  }

  async save(shipment: Shipment): Promise<Shipment> {
    const record = await this.prisma.shipment.upsert({
      where: { id: shipment.id },
      create: {
        id: shipment.id,
        prescriptionId: shipment.prescriptionId,
        patientId: shipment.patientId,
        pharmacyId: shipment.pharmacyId,
        medications: shipment.medications as any,
        shipmentStatus: shipment.shipmentStatus,
        trackingNumber: shipment.trackingNumber,
        address: shipment.address as any,
        version: shipment.version,
      },
      update: {
        shipmentStatus: shipment.shipmentStatus,
        trackingNumber: shipment.trackingNumber,
        updatedAt: new Date(),
        version: shipment.version,
      },
    });
    return this.toDomain(record);
  }

  async findById(id: string): Promise<Shipment | null> {
    const record = await this.prisma.shipment.findUnique({ where: { id } });
    return record ? this.toDomain(record) : null;
  }

  async findByPrescriptionId(prescriptionId: string): Promise<Shipment[]> {
    const records = await this.prisma.shipment.findMany({ where: { prescriptionId } });
    return records.map((r) => this.toDomain(r));
  }

  async findByPatientId(patientId: string): Promise<Shipment[]> {
    const records = await this.prisma.shipment.findMany({ where: { patientId } });
    return records.map((r) => this.toDomain(r));
  }

  async isMessageProcessed(messageId: string): Promise<boolean> {
    const record = await this.prisma.processedMessage.findUnique({ where: { id: messageId } });
    return !!record;
  }

  async markMessageProcessed(messageId: string, shipmentId?: string): Promise<void> {
    await this.prisma.processedMessage.create({
      data: { id: messageId, shipmentId },
    });
  }
}
