using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public class DeviceService(IDeviceRepository repository) : IDeviceService
{
    public Task<List<Device>> GetAllAsync(int? customerId = null, int? deviceTypeId = null) =>
        repository.GetAllAsync(customerId, deviceTypeId);

    public Task<List<Device>> SearchAsync(int customerId, string query, int limit) =>
        repository.SearchAsync(customerId, query, limit);

    public Task<Device?> GetByIdAsync(int id) => repository.GetByIdAsync(id);

    public async Task<DeviceOperationResult<Device>> CreateAsync(Device device)
    {
        var customer = await repository.GetCustomerAsync(device.CustomerId);
        if (customer is null)
            return DeviceOperationResult<Device>.Failure(DeviceOperationError.CustomerNotFound);

        var deviceType = await repository.GetDeviceTypeAsync(device.DeviceTypeId);
        if (deviceType is null)
            return DeviceOperationResult<Device>.Failure(DeviceOperationError.DeviceTypeNotFound);

        Normalize(device);
        device.Customer = customer;
        device.DeviceType = deviceType;
        await repository.AddAsync(device);
        return DeviceOperationResult<Device>.Success(device);
    }

    public async Task<DeviceOperationResult<Device>> UpdateAsync(int id, Device changes)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
            return DeviceOperationResult<Device>.Failure(DeviceOperationError.DeviceNotFound);

        var deviceType = await repository.GetDeviceTypeAsync(changes.DeviceTypeId);
        if (deviceType is null)
            return DeviceOperationResult<Device>.Failure(DeviceOperationError.DeviceTypeNotFound);

        existing.DeviceTypeId = changes.DeviceTypeId;
        existing.DeviceType = deviceType;
        existing.Brand = changes.Brand.Trim();
        existing.Model = changes.Model.Trim();
        existing.SerialNumber = NormalizeOptional(changes.SerialNumber);
        existing.Color = NormalizeOptional(changes.Color);
        existing.Description = NormalizeOptional(changes.Description);
        await repository.UpdateAsync(existing);
        return DeviceOperationResult<Device>.Success(existing);
    }

    public async Task<DeviceOperationResult<bool>> DeleteAsync(int id)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
            return DeviceOperationResult<bool>.Failure(DeviceOperationError.DeviceNotFound);

        if (await repository.HasRepairTicketsAsync(id))
            return DeviceOperationResult<bool>.Failure(DeviceOperationError.HasRepairTickets);

        await repository.DeleteAsync(existing);
        return DeviceOperationResult<bool>.Success(true);
    }

    private static void Normalize(Device device)
    {
        device.Brand = device.Brand.Trim();
        device.Model = device.Model.Trim();
        device.SerialNumber = NormalizeOptional(device.SerialNumber);
        device.Color = NormalizeOptional(device.Color);
        device.Description = NormalizeOptional(device.Description);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
