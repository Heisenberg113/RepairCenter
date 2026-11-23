namespace RepairCenter.Entities;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int SparePartId { get; set; }
    public required string TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public SparePart SparePart { get; set; } = null!;
}
