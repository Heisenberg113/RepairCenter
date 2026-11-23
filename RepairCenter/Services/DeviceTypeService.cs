using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public sealed class DeviceTypeService(IDeviceTypeRepository repository) : IDeviceTypeService
{
    public Task<List<DeviceType>> GetAllAsync() => repository.GetAllAsync();

    public Task<DeviceType?> GetByIdAsync(int id) => repository.GetByIdAsync(id);

    public async Task<DeviceTypeOperationResult<DeviceType>> CreateAsync(string name)
    {
        name = name.Trim();
        if (name.Length == 0) return DeviceTypeOperationResult<DeviceType>.Failure(DeviceTypeOperationError.InvalidName);
        if (await repository.NameExistsAsync(name))
            return DeviceTypeOperationResult<DeviceType>.Failure(DeviceTypeOperationError.DuplicateName);

        var deviceType = new DeviceType { Name = name };
        await repository.AddAsync(deviceType);
        return DeviceTypeOperationResult<DeviceType>.Success(deviceType);
    }

    public async Task<DeviceTypeOperationResult<DeviceType>> UpdateAsync(int id, string name)
    {
        var deviceType = await repository.GetByIdAsync(id);
        if (deviceType is null)
            return DeviceTypeOperationResult<DeviceType>.Failure(DeviceTypeOperationError.NotFound);

        name = name.Trim();
        if (name.Length == 0)
            return DeviceTypeOperationResult<DeviceType>.Failure(DeviceTypeOperationError.InvalidName);
        if (await repository.NameExistsAsync(name, id))
            return DeviceTypeOperationResult<DeviceType>.Failure(DeviceTypeOperationError.DuplicateName);

        deviceType.Name = name;
        await repository.SaveAsync();
        return DeviceTypeOperationResult<DeviceType>.Success(deviceType);
    }

    public async Task<DeviceTypeOperationError> DeleteAsync(int id)
    {
        var deviceType = await repository.GetByIdAsync(id);
        if (deviceType is null) return DeviceTypeOperationError.NotFound;
        if (await repository.IsInUseAsync(id)) return DeviceTypeOperationError.InUse;
        await repository.DeleteAsync(deviceType);
        return DeviceTypeOperationError.None;
    }
}
