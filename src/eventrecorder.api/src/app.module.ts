import { Module, MiddlewareConsumer, NestModule } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { CqrsModule } from '@nestjs/cqrs';
import { configuration } from './config/configuration';
import { CosmosService } from './infrastructure/database/cosmos.service';
import { EventRecordRepository } from './infrastructure/database/event-record.repository';
import { ServiceBusModule } from './infrastructure/messaging/service-bus.module';
import { DomainEventConsumer } from './messaging/domain-event.consumer';
import { RecordEventHandler } from './features/record-event/record-event.handler';
import {
  GetEventsByAggregateHandler,
  GetEventsByCorrelationHandler,
  GetEventsByDateRangeHandler,
  GetEventsByTypeHandler,
} from './features/get-events/get-events.handler';
import { GetEventsController } from './features/get-events/get-events.controller';
import { CorrelationIdMiddleware } from './common/correlation-id.middleware';

const CommandHandlers = [RecordEventHandler];

const QueryHandlers = [
  GetEventsByAggregateHandler,
  GetEventsByCorrelationHandler,
  GetEventsByDateRangeHandler,
  GetEventsByTypeHandler,
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
  controllers: [GetEventsController],
  providers: [
    CosmosService,
    EventRecordRepository,
    DomainEventConsumer,
    ...CommandHandlers,
    ...QueryHandlers,
  ],
})
export class AppModule implements NestModule {
  configure(consumer: MiddlewareConsumer) {
    consumer.apply(CorrelationIdMiddleware).forRoutes('*');
  }
}
