namespace RepairCenter.Utilities;

internal static class RepairTicketNumberGenerator
{
    public static string Generate() =>
        $"RC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();
}
