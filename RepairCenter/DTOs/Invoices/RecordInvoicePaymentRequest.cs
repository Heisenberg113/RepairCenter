using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.Invoices;

public class RecordInvoicePaymentRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Amount { get; set; }
    [Required, StringLength(40)]
    public string PaymentMethod { get; set; } = string.Empty;
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }
    [StringLength(500)]
    public string? Notes { get; set; }
}
