using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public class InventoryTransactionService(IInventoryTransactionRepository repository) : IInventoryTransactionService
{
    public async Task<InventoryTransactionResult<List<InventoryTransaction>>> GetAllAsync(
        int? sparePartId,
        string? type,
        DateTime? from,
        DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return InventoryTransactionResult<List<InventoryTransaction>>.Failure(InventoryTransactionError.InvalidDateRange);

        var normalizedType = NormalizeType(type);
        if (type is not null && normalizedType is null)
            return InventoryTransactionResult<List<InventoryTransaction>>.Failure(InventoryTransactionError.InvalidType);

        var transactions = await repository.GetAllAsync(sparePartId, normalizedType, from, to);
        return InventoryTransactionResult<List<InventoryTransaction>>.Success(transactions);
    }

    public Task<InventoryTransaction?> GetByIdAsync(int id) => repository.GetByIdAsync(id);

    public async Task<InventoryTransactionResult<InventoryTransaction>> CreateAsync(
        int sparePartId,
        string type,
        decimal quantity,
        decimal? unitCost,
        string? referenceNumber,
        string? note)
    {
        var normalizedType = NormalizeType(type);
        if (normalizedType is null)
            return InventoryTransactionResult<InventoryTransaction>.Failure(InventoryTransactionError.InvalidType);
        if (quantity <= 0)
            return InventoryTransactionResult<InventoryTransaction>.Failure(InventoryTransactionError.InvalidQuantity);
        if (unitCost < 0)
            return InventoryTransactionResult<InventoryTransaction>.Failure(InventoryTransactionError.InvalidUnitCost);

        var sparePart = await repository.GetSparePartAsync(sparePartId);
        if (sparePart is null)
            return InventoryTransactionResult<InventoryTransaction>.Failure(InventoryTransactionError.SparePartNotFound);
        if (!sparePart.IsActive)
            return InventoryTransactionResult<InventoryTransaction>.Failure(InventoryTransactionError.InactiveSparePart);

        var transaction = new InventoryTransaction
        {
            SparePartId = sparePartId,
            SparePart = sparePart,
            TransactionType = normalizedType,
            Quantity = quantity,
            UnitCost = unitCost ?? sparePart.UnitCost,
            ReferenceNumber = NormalizeOptional(referenceNumber),
            Note = NormalizeOptional(note),
            TransactionDate = DateTime.UtcNow
        };

        var applyError = await repository.ApplyAsync(
            transaction,
            isStockIn: normalizedType == "StockIn");

        var error = applyError switch
        {
            InventoryApplyError.None => InventoryTransactionError.None,
            InventoryApplyError.SparePartNotFound => InventoryTransactionError.SparePartNotFound,
            InventoryApplyError.InactiveSparePart => InventoryTransactionError.InactiveSparePart,
            InventoryApplyError.InsufficientStock => InventoryTransactionError.InsufficientStock,
            _ => InventoryTransactionError.SparePartNotFound
        };

        return error == InventoryTransactionError.None
            ? InventoryTransactionResult<InventoryTransaction>.Success(transaction)
            : InventoryTransactionResult<InventoryTransaction>.Failure(error);
    }

    private static string? NormalizeType(string? type)
    {
        if (string.Equals(type?.Trim(), "StockIn", StringComparison.OrdinalIgnoreCase))
            return "StockIn";
        if (string.Equals(type?.Trim(), "StockOut", StringComparison.OrdinalIgnoreCase))
            return "StockOut";
        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
