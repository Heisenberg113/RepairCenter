using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public interface IRepairServiceRepository
{
    Task<List<RepairService>> GetAllAsync(bool includeInactive);
    Task<RepairService?> GetByIdAsync(int id);
    Task<bool> DeviceTypeExistsAsync(int id);
    Task<bool> NameExistsAsync(string name, int? exceptId = null);
    Task<bool> IsInUseAsync(int id);
    Task AddAsync(RepairService service);
    Task SaveAsync();
    Task DeleteAsync(RepairService service);
}
