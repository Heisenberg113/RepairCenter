using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.InventoryTransactions;

public class CreateInventoryTransactionRequest
{
    [Range(1, int.MaxValue)]
    public int SparePartId { get; set; }

    [Required, RegularExpression("^(StockIn|StockOut)$", ErrorMessage = "Type must be StockIn or StockOut.")]
    public string Type { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? UnitCost { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
