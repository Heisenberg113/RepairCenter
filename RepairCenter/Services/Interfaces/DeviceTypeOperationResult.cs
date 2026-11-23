namespace RepairCenter.Services.Interfaces;

public enum DeviceTypeOperationError { None, NotFound, InvalidName, DuplicateName, InUse }

public sealed record DeviceTypeOperationResult<T>(T? Value, DeviceTypeOperationError Error)
{
    public static DeviceTypeOperationResult<T> Success(T value) => new(value, DeviceTypeOperationError.None);
    public static DeviceTypeOperationResult<T> Failure(DeviceTypeOperationError error) => new(default, error);
}
