using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public interface IInventoryTransactionRepository
{
    Task<List<InventoryTransaction>> GetAllAsync(
        int? sparePartId,
        string? type,
        DateTime? from,
        DateTime? to);
    Task<InventoryTransaction?> GetByIdAsync(int id);
    Task<SparePart?> GetSparePartAsync(int sparePartId);
    Task<InventoryApplyError> ApplyAsync(InventoryTransaction transaction, bool isStockIn);
}
