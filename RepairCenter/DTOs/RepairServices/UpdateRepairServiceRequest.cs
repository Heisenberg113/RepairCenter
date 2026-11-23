using System.ComponentModel.DataAnnotations;
using RepairCenter.Enums;

namespace RepairCenter.DTOs.RepairServices;

public sealed class UpdateRepairServiceRequest
{
    [Range(1, int.MaxValue)] public int DeviceTypeId { get; set; }

    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; }
    [Range(0, 36500)] public int WarrantyDuration { get; set; }
    public WarrantyDurationUnit WarrantyDurationUnit { get; set; } = WarrantyDurationUnit.Months;
    [StringLength(1000)] public string? WarrantyTerms { get; set; }
}
