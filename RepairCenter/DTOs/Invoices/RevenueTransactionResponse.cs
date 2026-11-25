namespace RepairCenter.DTOs.Invoices;

public class RevenueTransactionResponse
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime PaidAt { get; set; }
    public string? ProcessedByUserName { get; set; }
    public string? Notes { get; set; }
}
