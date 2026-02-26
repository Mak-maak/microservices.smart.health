import { Module, Global } from '@nestjs/common';
import { ServiceBusService } from './service-bus.service';

@Global()
@Module({
  providers: [ServiceBusService],
  exports: [ServiceBusService],
})
export class ServiceBusModule {}
