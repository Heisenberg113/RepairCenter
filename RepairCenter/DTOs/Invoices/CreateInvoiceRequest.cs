using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.Invoices;

public class CreateInvoiceRequest
{
    [Range(typeof(decimal), "0", "100")]
    public decimal DiscountPercent { get; set; }
    [Range(typeof(decimal), "0", "100")]
    public decimal TaxPercent { get; set; }
    public DateOnly? DueAt { get; set; }
    [StringLength(500)]
    public string? Notes { get; set; }
}
