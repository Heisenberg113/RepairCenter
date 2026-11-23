namespace RepairCenter.DTOs.CatalogChangeRequests;

public class CreateCatalogChangeRequest
{
    public string ItemType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public int? DeviceTypeId { get; set; }
    public string? PartNumber { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal? UnitPrice { get; set; }
    public int? WarrantyDuration { get; set; }
    public int? WarrantyDurationUnit { get; set; }
    public string? WarrantyTerms { get; set; }
}
