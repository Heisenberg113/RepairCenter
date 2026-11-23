namespace RepairCenter.Services.Interfaces;

public enum RepairTicketOperationError
{
    None,
    CustomerNotFound,
    DeviceNotFoundForCustomer,
    RepairTicketNotFound,
    TechnicianNotFoundOrInactive,
    TechnicianNotQualifiedForDevice,
    InvalidStatusTransition,
    ChangedByUserNotFoundOrInactive,
    TechnicianRequiredForStatus,
    TicketIsClosed,
    CannotUnassignTechnicianDuringWork
}

public sealed record RepairTicketOperationResult<T>(T? Value, RepairTicketOperationError Error)
{
    public static RepairTicketOperationResult<T> Success(T value) => new(value, RepairTicketOperationError.None);
    public static RepairTicketOperationResult<T> Failure(RepairTicketOperationError error) => new(default, error);
}
