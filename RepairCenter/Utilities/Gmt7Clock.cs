namespace RepairCenter.Utilities;

public static class Gmt7Clock
{
    private static readonly TimeSpan UtcOffset = TimeSpan.FromHours(7);

    public static DateOnly Today => FromUtc(DateTime.UtcNow);

    public static DateOnly FromUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return DateOnly.FromDateTime(utc + UtcOffset);
    }

    public static DateTime ToUtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    public static DateTime ToUtcStartOfDay(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue) - UtcOffset, DateTimeKind.Utc);

    public static DateTime ToUtcEndOfDay(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue) - UtcOffset, DateTimeKind.Utc);
}
