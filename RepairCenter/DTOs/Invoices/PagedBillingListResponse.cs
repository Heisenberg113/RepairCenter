namespace RepairCenter.DTOs.Invoices;

public sealed class PagedBillingListResponse
{
    public required IReadOnlyList<BillingListItemResponse> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
