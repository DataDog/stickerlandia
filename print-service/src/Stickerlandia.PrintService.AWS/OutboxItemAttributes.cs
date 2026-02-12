namespace Stickerlandia.PrintService.AWS;

public static class OutboxItemAttributes
{
    public const string PkPrefix = "OUTBOX#";
    public const string SkPrefix = "EVENT#";
    public const string ItemType = "ItemType";
    public const string ItemTypeValue = "OutboxItem";
    public const string EventType = "EventType";
    public const string EventData = "EventData";
    public const string EventTime = "EventTime";
    public const string TraceId = "TraceId";
    public const string Ttl = "TTL";
    public const string ItemId = "ItemId";
    public static readonly TimeSpan TtlDuration = TimeSpan.FromDays(7);
}
