export class RecordEventCommand {
  constructor(
    public readonly eventId: string,
    public readonly eventType: string,
    public readonly aggregateType: string,
    public readonly aggregateId: string,
    public readonly correlationId: string,
    public readonly sourceService: string,
    public readonly actorId: string,
    public readonly actorType: string,
    public readonly payload: Record<string, any>,
    public readonly metadata: Record<string, any>,
    public readonly occurredAt: string,
    public readonly version: number,
  ) {}
}
