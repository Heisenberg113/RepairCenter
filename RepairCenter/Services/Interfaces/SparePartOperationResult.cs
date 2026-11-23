namespace RepairCenter.Services.Interfaces;

public enum SparePartOperationError
{
    None,
    SparePartNotFound,
    DuplicatePartNumber,
    InactiveSparePart,
    InsufficientStock,
    InvalidWarrantyPolicy,
    InvalidDeviceType
}

public sealed record SparePartOperationResult<T>(T? Value, SparePartOperationError Error)
{
    public static SparePartOperationResult<T> Success(T value) => new(value, SparePartOperationError.None);
    public static SparePartOperationResult<T> Failure(SparePartOperationError error) => new(default, error);
}
