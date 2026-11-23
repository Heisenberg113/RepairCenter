using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public interface IDeviceRepository
{
    Task<List<Device>> GetAllAsync(int? customerId = null, int? deviceTypeId = null);
    Task<Device?> GetByIdAsync(int id);
    Task<Customer?> GetCustomerAsync(int customerId);
    Task<DeviceType?> GetDeviceTypeAsync(int deviceTypeId);
    Task<bool> HasRepairTicketsAsync(int deviceId);
    Task AddAsync(Device device);
    Task UpdateAsync(Device device);
    Task DeleteAsync(Device device);
}
