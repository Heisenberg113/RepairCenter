using RepairCenter.Entities;

namespace RepairCenter.Services.Interfaces;

public enum RepairServiceOperationError { None, NotFound, InvalidName, DuplicateName, InvalidPrice, InUse, InvalidWarranty, InvalidDeviceType }

public sealed record RepairServiceOperationResult<T>(T? Value, RepairServiceOperationError Error)
{
    public static RepairServiceOperationResult<T> Success(T value) => new(value, RepairServiceOperationError.None);
    public static RepairServiceOperationResult<T> Failure(RepairServiceOperationError error) => new(default, error);
}

public interface IRepairServiceService
{
    Task<List<RepairService>> GetAllAsync(bool includeInactive);
    Task<RepairService?> GetByIdAsync(int id);
    Task<RepairServiceOperationResult<RepairService>> CreateAsync(int deviceTypeId, string name, string? description, decimal unitPrice, int warrantyDuration, RepairCenter.Enums.WarrantyDurationUnit warrantyDurationUnit, string? warrantyTerms);
    Task<RepairServiceOperationResult<RepairService>> UpdateAsync(int id, int deviceTypeId, string name, string? description, decimal unitPrice, bool isActive, int warrantyDuration, RepairCenter.Enums.WarrantyDurationUnit warrantyDurationUnit, string? warrantyTerms);
    Task<RepairServiceOperationError> DeleteAsync(int id);
}
