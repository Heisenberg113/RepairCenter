using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public interface IDeviceTypeRepository
{
    Task<List<DeviceType>> GetAllAsync();
    Task<DeviceType?> GetByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int? exceptId = null);
    Task<bool> IsInUseAsync(int id);
    Task AddAsync(DeviceType deviceType);
    Task SaveAsync();
    Task DeleteAsync(DeviceType deviceType);
}
