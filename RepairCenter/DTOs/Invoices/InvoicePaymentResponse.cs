namespace RepairCenter.DTOs.Invoices;

public class InvoicePaymentResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? ProcessedByUserId { get; set; }
    public string? ProcessedByUserName { get; set; }
    public int? RefundOfPaymentId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
}
