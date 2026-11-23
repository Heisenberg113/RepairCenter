using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Invoices;
public class VoidInvoicePaymentRequest
{
    [Required, StringLength(40)] public string RefundMethod { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 3)] public string Reason { get; set; } = string.Empty;
}
