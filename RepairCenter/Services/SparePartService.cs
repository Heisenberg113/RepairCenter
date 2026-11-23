using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public class SparePartService(ISparePartRepository repository) : ISparePartService
{
    public Task<List<SparePart>> GetAllAsync(string? search, bool activeOnly, bool lowStockOnly) =>
        repository.GetAllAsync(search, activeOnly, lowStockOnly);

    public Task<SparePart?> GetByIdAsync(int id) => repository.GetByIdAsync(id);

    public async Task<SparePartOperationResult<SparePart>> CreateAsync(SparePart sparePart)
    {
        Normalize(sparePart);
        if (sparePart.DeviceTypeId is not int createDeviceTypeId || !await repository.DeviceTypeExistsAsync(createDeviceTypeId))
            return SparePartOperationResult<SparePart>.Failure(SparePartOperationError.InvalidDeviceType);
        if (!ValidWarrantyPolicy(sparePart.WarrantyDuration, sparePart.WarrantyDurationUnit))
            return SparePartOperationResult<SparePart>.Failure(SparePartOperationError.InvalidWarrantyPolicy);
        if (await repository.PartNumberExistsAsync(sparePart.PartNumber))
            return SparePartOperationResult<SparePart>.Failure(SparePartOperationError.DuplicatePartNumber);

        sparePart.QuantityInStock = 0;
        await repository.AddAsync(sparePart);
        return SparePartOperationResult<SparePart>.Success(sparePart);
    }

    public async Task<SparePartOperationResult<SparePart>> UpdateAsync(int id, SparePart changes)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
            return SparePartOperationResult<SparePart>.Failure(SparePartOperationError.SparePartNotFound);

        Normalize(changes);
        if (changes.DeviceTypeId is not int updateDeviceTypeId || !await repository.DeviceTypeExistsAsync(updateDeviceTypeId))
            return SparePartOperationResult<SparePart>.Failure(SparePartOperationError.InvalidDeviceType);
        if (!ValidWarrantyPolicy(changes.WarrantyDuration, changes.WarrantyDurationUnit))
            return SparePartOperationResult<SparePart>.Failure(SparePartOperationError.InvalidWarrantyPolicy);
        if (await repository.PartNumberExistsAsync(changes.PartNumber, id))
            return SparePartOperationResult<SparePart>.Failure(SparePartOperationError.DuplicatePartNumber);

        existing.PartNumber = changes.PartNumber;
        existing.Name = changes.Name;
        existing.DeviceTypeId = changes.DeviceTypeId;
        existing.Description = changes.Description;
        existing.UnitCost = changes.UnitCost;
        existing.SellingPrice = changes.SellingPrice;
        existing.ReorderLevel = changes.ReorderLevel;
        existing.IsActive = changes.IsActive;
        existing.WarrantyDuration = changes.WarrantyDuration;
        existing.WarrantyDurationUnit = changes.WarrantyDurationUnit;
        existing.WarrantyTerms = NormalizeOptional(changes.WarrantyTerms);
        await repository.UpdateAsync(existing);
        return SparePartOperationResult<SparePart>.Success(existing);
    }

    public async Task<SparePartOperationResult<bool>> DeactivateAsync(int id)
    {
        var sparePart = await repository.GetByIdAsync(id);
        if (sparePart is null)
            return SparePartOperationResult<bool>.Failure(SparePartOperationError.SparePartNotFound);

        sparePart.IsActive = false;
        await repository.UpdateAsync(sparePart);
        return SparePartOperationResult<bool>.Success(true);
    }

    private static void Normalize(SparePart sparePart)
    {
        sparePart.PartNumber = sparePart.PartNumber.Trim().ToUpperInvariant();
        sparePart.Name = sparePart.Name.Trim();
        sparePart.Description = NormalizeOptional(sparePart.Description);
        sparePart.WarrantyTerms = NormalizeOptional(sparePart.WarrantyTerms);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool ValidWarrantyPolicy(int duration, RepairCenter.Enums.WarrantyDurationUnit unit) => unit switch
    {
        RepairCenter.Enums.WarrantyDurationUnit.Days => duration is >= 0 and <= 36500,
        RepairCenter.Enums.WarrantyDurationUnit.Months => duration is >= 0 and <= 120,
        _ => false
    };
}
