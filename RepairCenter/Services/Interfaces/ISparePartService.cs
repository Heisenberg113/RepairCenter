using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces;

public interface ISparePartService
{
    Task<List<SparePart>> GetAllAsync(string? search, bool activeOnly, bool lowStockOnly);
    Task<SparePart?> GetByIdAsync(int id);
    Task<SparePartOperationResult<SparePart>> CreateAsync(SparePart sparePart);
    Task<SparePartOperationResult<SparePart>> UpdateAsync(int id, SparePart changes);
    Task<SparePartOperationResult<bool>> DeactivateAsync(int id);
}
