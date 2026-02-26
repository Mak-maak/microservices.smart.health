export interface EventRecordDocument {
  id: string;
  eventId: string;
  eventType: string;
  aggregateType: string;
  aggregateId: string;
  correlationId: string;
  sourceService: string;
  actorId: string;
  actorType: string;
  payload: Record<string, any>;
  metadata: Record<string, any>;
  occurredAt: string;
  recordedAt: string;
  version: number;
}
