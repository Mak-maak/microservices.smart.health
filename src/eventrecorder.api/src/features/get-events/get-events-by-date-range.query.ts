export class GetEventsByDateRangeQuery {
  constructor(
    public readonly from: string,
    public readonly to: string,
  ) {}
}
