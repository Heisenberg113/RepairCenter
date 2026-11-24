namespace RepairCenter.DTOs.Invoices;

public sealed class BillingListItemResponse
{
    public int TicketId { get; init; }
    public required string TicketNumber { get; init; }
    public required string CustomerName { get; init; }
    public required string DeviceBrand { get; init; }
    public required string DeviceModel { get; init; }
    public BillingInvoiceSummaryResponse? Invoice { get; init; }
}
