namespace RepairCenter.Contracts;

public sealed record PagedRepairTicketResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int ActiveCount);
