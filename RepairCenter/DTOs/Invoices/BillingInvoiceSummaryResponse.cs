using RepairCenter.Enums;

namespace RepairCenter.DTOs.Invoices;

public sealed class BillingInvoiceSummaryResponse
{
    public int Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public decimal OutstandingAmount { get; init; }
    public DateOnly? DueAt { get; init; }
}
