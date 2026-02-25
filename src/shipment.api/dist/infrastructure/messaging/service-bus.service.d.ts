import { OnModuleInit, OnModuleDestroy } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { ServiceBusClient } from '@azure/service-bus';
export declare class ServiceBusService implements OnModuleInit, OnModuleDestroy {
    private readonly configService;
    private readonly logger;
    private client;
    private senders;
    private readonly connectionString;
    constructor(configService: ConfigService);
    onModuleInit(): void;
    onModuleDestroy(): Promise<void>;
    publishEvent(topicName: string, eventType: string, payload: any, correlationId?: string): Promise<void>;
    getClient(): ServiceBusClient | null;
}
