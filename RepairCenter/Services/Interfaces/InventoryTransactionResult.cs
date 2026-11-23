namespace RepairCenter.Services.Interfaces;

public enum InventoryTransactionError
{
    None,
    TransactionNotFound,
    SparePartNotFound,
    InactiveSparePart,
    InsufficientStock,
    InvalidType,
    InvalidQuantity,
    InvalidUnitCost,
    InvalidDateRange
}

public sealed record InventoryTransactionResult<T>(T? Value, InventoryTransactionError Error)
{
    public static InventoryTransactionResult<T> Success(T value) => new(value, InventoryTransactionError.None);
    public static InventoryTransactionResult<T> Failure(InventoryTransactionError error) => new(default, error);
}
