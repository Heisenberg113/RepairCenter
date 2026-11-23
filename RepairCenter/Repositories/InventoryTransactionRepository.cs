using System.Data;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class InventoryTransactionRepository(RepairCenterDbContext context) : IInventoryTransactionRepository
{
    public async Task<List<InventoryTransaction>> GetAllAsync(
        int? sparePartId,
        string? type,
        DateTime? from,
        DateTime? to)
    {
        var query = context.InventoryTransactions
            .AsNoTracking()
            .Include(transaction => transaction.SparePart)
            .AsQueryable();

        if (sparePartId.HasValue)
            query = query.Where(transaction => transaction.SparePartId == sparePartId.Value);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(transaction => transaction.TransactionType == type);
        if (from.HasValue)
            query = query.Where(transaction => transaction.TransactionDate >= from.Value);
        if (to.HasValue)
            query = query.Where(transaction => transaction.TransactionDate <= to.Value);

        return await query.OrderByDescending(transaction => transaction.TransactionDate).ToListAsync();
    }

    public Task<InventoryTransaction?> GetByIdAsync(int id) => context.InventoryTransactions
        .AsNoTracking()
        .Include(transaction => transaction.SparePart)
        .FirstOrDefaultAsync(transaction => transaction.Id == id);

    public Task<SparePart?> GetSparePartAsync(int sparePartId) => context.SpareParts
        .FirstOrDefaultAsync(part => part.Id == sparePartId);

    public async Task<InventoryApplyError> ApplyAsync(InventoryTransaction transaction, bool isStockIn)
    {
        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var query = context.SpareParts.Where(part => part.Id == transaction.SparePartId && part.IsActive);

        var updatedRows = isStockIn
            ? await query.ExecuteUpdateAsync(setters => setters.SetProperty(
                part => part.QuantityInStock,
                part => part.QuantityInStock + transaction.Quantity))
            : await query.Where(part => part.QuantityInStock >= transaction.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    part => part.QuantityInStock,
                    part => part.QuantityInStock - transaction.Quantity));

        if (updatedRows == 0)
        {
            var part = await context.SpareParts.AsNoTracking()
                .Where(item => item.Id == transaction.SparePartId)
                .Select(item => new { item.Id, item.IsActive })
                .FirstOrDefaultAsync();
            await dbTransaction.RollbackAsync();

            if (part is null)
                return InventoryApplyError.SparePartNotFound;
            if (!part.IsActive)
                return InventoryApplyError.InactiveSparePart;
            return InventoryApplyError.InsufficientStock;
        }

        await context.InventoryTransactions.AddAsync(transaction);
        await context.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        return InventoryApplyError.None;
    }
}
