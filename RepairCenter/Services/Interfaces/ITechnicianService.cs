using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces;

public interface ITechnicianService
{
    Task<List<Technician>> GetAllAsync();
    Task<Technician?> GetByIdAsync(int id);
    Task<List<DeviceType>> GetDeviceTypesAsync();
    Task<TechnicianOperationResult<Technician>> CreateAsync(Technician technician, IReadOnlyCollection<int> deviceTypeIds);
    Task<TechnicianOperationResult<Technician>> CreateWithAccountAsync(
        Technician technician, IReadOnlyCollection<int> deviceTypeIds, string userName);
    Task<TechnicianOperationResult<Technician>> UpdateAsync(int id, Technician changes, IReadOnlyCollection<int> deviceTypeIds);
    Task<TechnicianOperationResult<bool>> DeleteAsync(int id);
}
