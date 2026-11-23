using RepairCenter.Enums;

namespace RepairCenter.DTOs.RepairServices;

public sealed class RepairServiceResponse
{
    public int Id { get; set; }
    public int? DeviceTypeId { get; set; }
    public string? DeviceTypeName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public int WarrantyDuration { get; set; }
    public WarrantyDurationUnit WarrantyDurationUnit { get; set; }
    public string? WarrantyTerms { get; set; }
}
