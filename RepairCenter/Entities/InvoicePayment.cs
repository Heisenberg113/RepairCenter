using RepairCenter.Enums;

namespace RepairCenter.Entities;

public class InvoicePayment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public InvoicePaymentType Type { get; set; } = InvoicePaymentType.Payment;
    public int? ProcessedByUserId { get; set; }
    public int? RefundOfPaymentId { get; set; }
    public required string PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public User? ProcessedByUser { get; set; }
    public InvoicePayment? RefundOfPayment { get; set; }
    public ICollection<InvoicePayment> Refunds { get; set; } = new List<InvoicePayment>();
}
