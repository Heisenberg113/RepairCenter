namespace RepairCenter.Services.Interfaces;

public enum DeviceOperationError
{
    None,
    DeviceNotFound,
    CustomerNotFound,
    DeviceTypeNotFound,
    HasRepairTickets
}

public sealed record DeviceOperationResult<T>(T? Value, DeviceOperationError Error)
{
    public static DeviceOperationResult<T> Success(T value) => new(value, DeviceOperationError.None);
    public static DeviceOperationResult<T> Failure(DeviceOperationError error) => new(default, error);
}
