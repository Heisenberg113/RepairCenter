using RepairCenter.Enums;

namespace RepairCenter.DTOs.SpareParts;

public class SparePartResponse
{
    public int Id { get; set; }
    public int? DeviceTypeId { get; set; }
    public string? DeviceTypeName { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal QuantityInStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsActive { get; set; }
    public int WarrantyDuration { get; set; }
    public WarrantyDurationUnit WarrantyDurationUnit { get; set; }
    public string? WarrantyTerms { get; set; }
}
