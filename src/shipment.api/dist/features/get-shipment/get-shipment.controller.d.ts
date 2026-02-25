import { QueryBus } from '@nestjs/cqrs';
export declare class GetShipmentController {
    private readonly queryBus;
    private readonly logger;
    constructor(queryBus: QueryBus);
    getByPrescription(prescriptionId: string): Promise<any>;
    getByPatient(patientId: string): Promise<any>;
    getById(id: string): Promise<any>;
}
