namespace RepairCenter.Services.Interfaces;

public enum TechnicianOperationError
{
    None,
    TechnicianNotFound,
    UserNotFoundOrAlreadyLinked,
    DuplicateEmployeeCode,
    DeviceTypesNotFound,
    HasRepairTickets,
    AccountRequiresTechnicianProfile,
    DuplicateAccountIdentity,
    TechnicianAccountSetupFailed,
    CannotLinkAdministratorAccount,
    TechnicianAccountLinkRequiresActiveEntities
}

public sealed record TechnicianOperationResult<T>(
    T? Value,
    TechnicianOperationError Error,
    string? TemporaryPassword = null)
{
    public static TechnicianOperationResult<T> Success(T value) => new(value, TechnicianOperationError.None);
    public static TechnicianOperationResult<T> SuccessWithTemporaryPassword(T value, string temporaryPassword) =>
        new(value, TechnicianOperationError.None, temporaryPassword);
    public static TechnicianOperationResult<T> Failure(TechnicianOperationError error) => new(default, error);
}
