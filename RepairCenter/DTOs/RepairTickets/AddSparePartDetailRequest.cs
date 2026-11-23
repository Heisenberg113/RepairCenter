using System.ComponentModel.DataAnnotations;
using RepairCenter.Enums;

namespace RepairCenter.DTOs.RepairTickets;

public class AddSparePartDetailRequest
{
    [Range(1, int.MaxValue)]
    public int SparePartId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0", "100")]
    public decimal DiscountPercent { get; set; }

    [Range(0, 36500)] public int? WarrantyDuration { get; set; }
    public WarrantyDurationUnit? WarrantyDurationUnit { get; set; }
    [StringLength(1000)] public string? WarrantyTerms { get; set; }
}
