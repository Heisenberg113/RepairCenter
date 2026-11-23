namespace RepairCenter.Repositories.Interfaces;

public enum InventoryApplyError
{
    None,
    SparePartNotFound,
    InactiveSparePart,
    InsufficientStock
}
