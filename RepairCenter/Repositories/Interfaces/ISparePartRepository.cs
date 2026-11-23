using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public interface ISparePartRepository
{
    Task<List<SparePart>> GetAllAsync(string? search, bool activeOnly, bool lowStockOnly);
    Task<SparePart?> GetByIdAsync(int id);
    Task<bool> DeviceTypeExistsAsync(int id);
    Task<bool> PartNumberExistsAsync(string partNumber, int? exceptId = null);
    Task AddAsync(SparePart sparePart);
    Task UpdateAsync(SparePart sparePart);
}
