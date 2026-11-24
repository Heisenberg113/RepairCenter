using Microsoft.EntityFrameworkCore;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public sealed class RepairServiceService(IRepairServiceRepository repository) : IRepairServiceService
{
    public Task<List<RepairService>> GetAllAsync(bool includeInactive) => repository.GetAllAsync(includeInactive);
    public Task<RepairService?> GetByIdAsync(int id) => repository.GetByIdAsync(id);

    public async Task<RepairServiceOperationResult<RepairService>> CreateAsync(int deviceTypeId, string name, string? description, decimal unitPrice, int warrantyDuration, RepairCenter.Enums.WarrantyDurationUnit warrantyDurationUnit, string? warrantyTerms)
    {
        if (!await repository.DeviceTypeExistsAsync(deviceTypeId)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidDeviceType);
        name = name.Trim();
        if (name.Length == 0) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidName);
        if (unitPrice < 0 || unitPrice != decimal.Round(unitPrice, 2)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidPrice);
        if (await repository.NameExistsAsync(name)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.DuplicateName);
        if (!ValidWarranty(warrantyDuration, warrantyDurationUnit)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidWarranty);
        var service = new RepairService { DeviceTypeId = deviceTypeId, Name = name, Description = Normalize(description), UnitPrice = unitPrice, WarrantyDuration = warrantyDuration, WarrantyDurationUnit = warrantyDurationUnit, WarrantyTerms = Normalize(warrantyTerms) };
        try
        {
            await repository.AddAsync(service);
        }
        catch (DbUpdateException)
        {
            if (await repository.NameExistsAsync(name))
                return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.DuplicateName);
            throw;
        }
        return RepairServiceOperationResult<RepairService>.Success(service);
    }

    public async Task<RepairServiceOperationResult<RepairService>> UpdateAsync(int id, int deviceTypeId, string name, string? description, decimal unitPrice, bool isActive, int warrantyDuration, RepairCenter.Enums.WarrantyDurationUnit warrantyDurationUnit, string? warrantyTerms)
    {
        var service = await repository.GetByIdAsync(id);
        if (service is null) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.NotFound);
        if (!await repository.DeviceTypeExistsAsync(deviceTypeId)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidDeviceType);
        name = name.Trim();
        if (name.Length == 0) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidName);
        if (unitPrice < 0 || unitPrice != decimal.Round(unitPrice, 2)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidPrice);
        if (!ValidWarranty(warrantyDuration, warrantyDurationUnit)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.InvalidWarranty);
        if (await repository.NameExistsAsync(name, id)) return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.DuplicateName);
        service.Name = name;
        service.DeviceTypeId = deviceTypeId;
        service.Description = Normalize(description);
        service.UnitPrice = unitPrice;
        service.IsActive = isActive;
        service.WarrantyDuration = warrantyDuration;
        service.WarrantyDurationUnit = warrantyDurationUnit;
        service.WarrantyTerms = Normalize(warrantyTerms);
        try
        {
            await repository.SaveAsync();
        }
        catch (DbUpdateException)
        {
            if (await repository.NameExistsAsync(name, id))
                return RepairServiceOperationResult<RepairService>.Failure(RepairServiceOperationError.DuplicateName);
            throw;
        }
        return RepairServiceOperationResult<RepairService>.Success(service);
    }

    public async Task<RepairServiceOperationError> DeleteAsync(int id)
    {
        var service = await repository.GetByIdAsync(id);
        if (service is null) return RepairServiceOperationError.NotFound;
        if (await repository.IsInUseAsync(id)) return RepairServiceOperationError.InUse;
        await repository.DeleteAsync(service);
        return RepairServiceOperationError.None;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidWarranty(int duration, RepairCenter.Enums.WarrantyDurationUnit unit) => unit switch
    {
        RepairCenter.Enums.WarrantyDurationUnit.Days => duration is >= 0 and <= 36500,
        RepairCenter.Enums.WarrantyDurationUnit.Months => duration is >= 0 and <= 120,
        _ => false
    };
}
