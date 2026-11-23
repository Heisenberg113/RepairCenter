using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces;

public interface IDeviceTypeService
{
    Task<List<DeviceType>> GetAllAsync();
    Task<DeviceType?> GetByIdAsync(int id);
    Task<DeviceTypeOperationResult<DeviceType>> CreateAsync(string name);
    Task<DeviceTypeOperationResult<DeviceType>> UpdateAsync(int id, string name);
    Task<DeviceTypeOperationError> DeleteAsync(int id);
}
