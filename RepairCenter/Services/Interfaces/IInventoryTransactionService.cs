using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces;

public interface IInventoryTransactionService
{
    Task<InventoryTransactionResult<List<InventoryTransaction>>> GetAllAsync(
        int? sparePartId,
        string? type,
        DateTime? from,
        DateTime? to);
    Task<InventoryTransaction?> GetByIdAsync(int id);
    Task<InventoryTransactionResult<InventoryTransaction>> CreateAsync(
        int sparePartId,
        string type,
        decimal quantity,
        decimal? unitCost,
        string? referenceNumber,
        string? note);
}
