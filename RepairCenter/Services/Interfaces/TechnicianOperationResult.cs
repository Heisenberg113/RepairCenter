namespace RepairCenter.Services.Interfaces;

public enum TechnicianOperationError
{
    None,
    TechnicianNotFound,
    UserNotFoundOrAlreadyLinked,
    DuplicateEmployeeCode,
    DeviceTypesNotFound,
    HasRepairTickets,
    AccountRequiresTechnicianProfile
}

public sealed record TechnicianOperationResult<T>(T? Value, TechnicianOperationError Error)
{
    public static TechnicianOperationResult<T> Success(T value) => new(value, TechnicianOperationError.None);
    public static TechnicianOperationResult<T> Failure(TechnicianOperationError error) => new(default, error);
}
