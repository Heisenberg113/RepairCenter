namespace RepairCenter.DTOs.SpareParts;

public class InventoryTransactionResponse
{
    public int Id { get; set; }
    public int SparePartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Note { get; set; }
}
