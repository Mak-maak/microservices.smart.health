import { Module, MiddlewareConsumer, NestModule } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { CqrsModule } from '@nestjs/cqrs';
import { configuration } from './config/configuration';
import { PrismaService } from './infrastructure/database/prisma.service';
import { ShipmentRepository } from './infrastructure/database/shipment.repository';
import { ServiceBusModule } from './infrastructure/messaging/service-bus.module';
import { ShipmentEventPublisher } from './messaging/publishers/shipment-event.publisher';
import { PrescriptionCreatedConsumer } from './messaging/consumers/prescription-created.consumer';
import { MedicinesPrescribedConsumer } from './messaging/consumers/medicines-prescribed.consumer';
import { CreateShipmentHandler } from './features/create-shipment/create-shipment.handler';
import { DispatchShipmentHandler } from './features/dispatch-shipment/dispatch-shipment.handler';
import { DispatchShipmentController } from './features/dispatch-shipment/dispatch-shipment.controller';
import { MarkDeliveredHandler } from './features/mark-delivered/mark-delivered.handler';
import { MarkDeliveredController } from './features/mark-delivered/mark-delivered.controller';
import { FailShipmentHandler } from './features/fail-shipment/fail-shipment.handler';
import { GetShipmentByIdHandler, GetShipmentsByPrescriptionHandler, GetShipmentsByPatientHandler } from './features/get-shipment/get-shipment.handler';
import { GetShipmentController } from './features/get-shipment/get-shipment.controller';
import { CorrelationIdMiddleware } from './common/correlation-id.middleware';

const CommandHandlers = [
  CreateShipmentHandler,
  DispatchShipmentHandler,
  MarkDeliveredHandler,
  FailShipmentHandler,
];

const QueryHandlers = [
  GetShipmentByIdHandler,
  GetShipmentsByPrescriptionHandler,
  GetShipmentsByPatientHandler,
];

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      load: [configuration],
    }),
    CqrsModule,
    ServiceBusModule,
  ],
  controllers: [
    DispatchShipmentController,
    MarkDeliveredController,
    GetShipmentController,
  ],
  providers: [
    PrismaService,
    ShipmentRepository,
    ShipmentEventPublisher,
    PrescriptionCreatedConsumer,
    MedicinesPrescribedConsumer,
    ...CommandHandlers,
    ...QueryHandlers,
  ],
})
export class AppModule implements NestModule {
  configure(consumer: MiddlewareConsumer) {
    consumer.apply(CorrelationIdMiddleware).forRoutes('*');
  }
}
