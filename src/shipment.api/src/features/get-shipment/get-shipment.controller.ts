import { Controller, Get, Param, Logger } from '@nestjs/common';
import { QueryBus } from '@nestjs/cqrs';
import { GetShipmentByIdQuery } from './get-shipment-by-id.query';
import { GetShipmentsByPrescriptionQuery } from './get-shipments-by-prescription.query';
import { GetShipmentsByPatientQuery } from './get-shipments-by-patient.query';

@Controller('api/shipments')
export class GetShipmentController {
  private readonly logger = new Logger(GetShipmentController.name);

  constructor(private readonly queryBus: QueryBus) {}

  @Get('prescription/:prescriptionId')
  async getByPrescription(@Param('prescriptionId') prescriptionId: string) {
    return this.queryBus.execute(new GetShipmentsByPrescriptionQuery(prescriptionId));
  }

  @Get('patient/:patientId')
  async getByPatient(@Param('patientId') patientId: string) {
    return this.queryBus.execute(new GetShipmentsByPatientQuery(patientId));
  }

  @Get(':id')
  async getById(@Param('id') id: string) {
    return this.queryBus.execute(new GetShipmentByIdQuery(id));
  }
}
