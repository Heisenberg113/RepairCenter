namespace RepairCenter.DTOs.RepairTickets;

public sealed class PagedRepairTicketResponse
{
    public required IEnumerable<RepairTicketResponse> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
}
