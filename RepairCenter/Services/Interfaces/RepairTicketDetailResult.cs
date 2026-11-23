namespace RepairCenter.Services.Interfaces;

public enum RepairTicketDetailError
{
    None,
    RepairTicketNotFound,
    RepairTicketClosed,
    RepairServiceNotFoundOrInactive,
    SparePartNotFound,
    SparePartInactive,
    InsufficientStock,
    DiscountExceedsSubtotal,
    RepairTicketNotEditable,
    DetailNotFound,
    InvalidAmounts,
    InvalidWarrantyPolicy
}

public sealed record RepairTicketDetailResult<T>(T? Value, RepairTicketDetailError Error)
{
    public static RepairTicketDetailResult<T> Success(T value) => new(value, RepairTicketDetailError.None);
    public static RepairTicketDetailResult<T> Failure(RepairTicketDetailError error) => new(default, error);
}
