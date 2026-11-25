using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces;

public interface IDeviceService
{
    Task<List<Device>> GetAllAsync(int? customerId = null, int? deviceTypeId = null);
    Task<List<Device>> SearchAsync(int customerId, string query, int limit);
    Task<Device?> GetByIdAsync(int id);
    Task<DeviceOperationResult<Device>> CreateAsync(Device device);
    Task<DeviceOperationResult<Device>> UpdateAsync(int id, Device changes);
    Task<DeviceOperationResult<bool>> DeleteAsync(int id);
}
