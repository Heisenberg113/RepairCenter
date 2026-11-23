using System.ComponentModel.DataAnnotations;
using RepairCenter.Enums;

namespace RepairCenter.DTOs.SpareParts;

public class UpdateSparePartRequest
{
    [Range(1, int.MaxValue)] public int DeviceTypeId { get; set; }

    [Required, StringLength(50, MinimumLength = 1)]
    public string PartNumber { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal UnitCost { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal SellingPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; }
    [Range(0, 36500)] public int WarrantyDuration { get; set; }
    public WarrantyDurationUnit WarrantyDurationUnit { get; set; } = WarrantyDurationUnit.Months;
    [StringLength(1000)] public string? WarrantyTerms { get; set; }
}
