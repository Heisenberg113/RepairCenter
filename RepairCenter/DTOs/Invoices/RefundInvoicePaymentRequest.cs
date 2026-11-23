using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Invoices;
public class RefundInvoicePaymentRequest
{
    [Range(1, int.MaxValue)] public int PaymentId { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Amount { get; set; }
    [Required, StringLength(40)] public string RefundMethod { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 3)] public string Reason { get; set; } = string.Empty;
}
