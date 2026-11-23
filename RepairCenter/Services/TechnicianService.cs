using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public class TechnicianService(ITechnicianRepository repository) : ITechnicianService
{
    public Task<List<Technician>> GetAllAsync() => repository.GetAllAsync();

    public Task<Technician?> GetByIdAsync(int id) => repository.GetByIdAsync(id);

    public Task<List<DeviceType>> GetDeviceTypesAsync() => repository.GetAllDeviceTypesAsync();

    public async Task<TechnicianOperationResult<Technician>> CreateAsync(
        Technician technician,
        IReadOnlyCollection<int> deviceTypeIds)
    {
        Normalize(technician);

        if (await repository.EmployeeCodeExistsAsync(technician.EmployeeCode))
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.DuplicateEmployeeCode);

        if (!await repository.UserAvailableAsync(technician.UserId))
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.UserNotFoundOrAlreadyLinked);

        var deviceTypes = await ResolveDeviceTypesAsync(deviceTypeIds);
        if (deviceTypes is null)
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.DeviceTypesNotFound);

        technician.DeviceTypes = deviceTypes.Select(type => new TechnicianDeviceType
        {
            Technician = technician,
            DeviceType = type
        }).ToList();

        await repository.AddAsync(technician);
        return TechnicianOperationResult<Technician>.Success(technician);
    }

    public async Task<TechnicianOperationResult<Technician>> UpdateAsync(
        int id,
        Technician changes,
        IReadOnlyCollection<int> deviceTypeIds)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.TechnicianNotFound);

        if (existing.UserId.HasValue && existing.UserId != changes.UserId &&
            await repository.UserHasTechnicianRoleAsync(existing.UserId.Value))
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.AccountRequiresTechnicianProfile);

        Normalize(changes);

        if (await repository.EmployeeCodeExistsAsync(changes.EmployeeCode, id))
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.DuplicateEmployeeCode);

        if (!await repository.UserAvailableAsync(changes.UserId, id))
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.UserNotFoundOrAlreadyLinked);

        var deviceTypes = await ResolveDeviceTypesAsync(deviceTypeIds);
        if (deviceTypes is null)
            return TechnicianOperationResult<Technician>.Failure(TechnicianOperationError.DeviceTypesNotFound);

        existing.UserId = changes.UserId;
        existing.EmployeeCode = changes.EmployeeCode;
        existing.FullName = changes.FullName;
        existing.PhoneNumber = changes.PhoneNumber;
        existing.Email = changes.Email;
        existing.Specialty = changes.Specialty;
        existing.IsActive = changes.IsActive;

        var requestedTypeIds = deviceTypes.Select(type => type.Id).ToHashSet();
        var removedLinks = existing.DeviceTypes
            .Where(link => !requestedTypeIds.Contains(link.DeviceTypeId))
            .ToList();
        foreach (var link in removedLinks)
            existing.DeviceTypes.Remove(link);

        var existingTypeIds = existing.DeviceTypes.Select(link => link.DeviceTypeId).ToHashSet();
        foreach (var deviceType in deviceTypes)
        {
            if (existingTypeIds.Contains(deviceType.Id))
                continue;

            existing.DeviceTypes.Add(new TechnicianDeviceType
            {
                TechnicianId = existing.Id,
                DeviceTypeId = deviceType.Id,
                Technician = existing,
                DeviceType = deviceType
            });
        }

        await repository.UpdateAsync(existing);
        return TechnicianOperationResult<Technician>.Success(existing);
    }

    public async Task<TechnicianOperationResult<bool>> DeleteAsync(int id)
    {
        var technician = await repository.GetByIdAsync(id);
        if (technician is null)
            return TechnicianOperationResult<bool>.Failure(TechnicianOperationError.TechnicianNotFound);

        if (await repository.HasRepairTicketsAsync(id))
            return TechnicianOperationResult<bool>.Failure(TechnicianOperationError.HasRepairTickets);
        if (technician.UserId.HasValue && await repository.UserHasTechnicianRoleAsync(technician.UserId.Value))
            return TechnicianOperationResult<bool>.Failure(TechnicianOperationError.AccountRequiresTechnicianProfile);

        await repository.DeleteAsync(technician);
        return TechnicianOperationResult<bool>.Success(true);
    }

    private async Task<List<DeviceType>?> ResolveDeviceTypesAsync(IReadOnlyCollection<int> ids)
    {
        var distinctIds = ids.Distinct().ToArray();
        var deviceTypes = await repository.GetDeviceTypesByIdsAsync(distinctIds);
        return deviceTypes.Count == distinctIds.Length ? deviceTypes : null;
    }

    private static void Normalize(Technician technician)
    {
        technician.EmployeeCode = technician.EmployeeCode.Trim();
        technician.FullName = technician.FullName.Trim();
        technician.PhoneNumber = NormalizeOptional(technician.PhoneNumber);
        technician.Email = NormalizeOptional(technician.Email);
        technician.Specialty = NormalizeOptional(technician.Specialty);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
