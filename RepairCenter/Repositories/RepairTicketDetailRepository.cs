using System.Data;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class RepairTicketDetailRepository(RepairCenterDbContext context) : IRepairTicketDetailRepository
{
    public Task<bool> TicketExistsAsync(int repairTicketId) =>
        context.RepairTickets.AnyAsync(ticket => ticket.Id == repairTicketId);

    public Task<RepairTicket?> GetTicketAsync(int repairTicketId) => context.RepairTickets
        .AsNoTracking()
        .FirstOrDefaultAsync(ticket => ticket.Id == repairTicketId);

    public Task<List<RepairTicketDetail>> GetByTicketAsync(int repairTicketId) => context.RepairTicketDetails
        .AsNoTracking()
        .Include(detail => detail.RepairService)
        .Include(detail => detail.SparePart)
        .Where(detail => detail.RepairTicketId == repairTicketId)
        .OrderBy(detail => detail.Id)
        .ToListAsync();

    public Task<RepairService?> GetActiveRepairServiceAsync(int repairServiceId) => context.RepairServices
        .AsNoTracking()
        .FirstOrDefaultAsync(service => service.Id == repairServiceId && service.IsActive);

    public Task<SparePart?> GetSparePartAsync(int sparePartId) => context.SpareParts
        .AsNoTracking()
        .FirstOrDefaultAsync(part => part.Id == sparePartId);

    public async Task<AddTicketDetailError> AddAsync(
        RepairTicketDetail detail,
        InventoryTransaction? inventoryTransaction)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ticket = await context.RepairTickets
            .FirstOrDefaultAsync(item => item.Id == detail.RepairTicketId);

        if (ticket is null)
        {
            await transaction.RollbackAsync();
            return AddTicketDetailError.RepairTicketNotFound;
        }

        if (ticket.Status is RepairStatus.Completed or RepairStatus.Delivered or RepairStatus.Cancelled)
        {
            await transaction.RollbackAsync();
            return AddTicketDetailError.RepairTicketClosed;
        }

        if (detail.RepairServiceId.HasValue)
        {
            var serviceIsActive = await context.RepairServices.AnyAsync(service =>
                service.Id == detail.RepairServiceId.Value && service.IsActive);
            if (!serviceIsActive)
            {
                await transaction.RollbackAsync();
                return AddTicketDetailError.RepairServiceNotFoundOrInactive;
            }
        }
        else if (detail.SparePartId.HasValue)
        {
            var updateCount = await context.SpareParts
                .Where(part => part.Id == detail.SparePartId.Value && part.IsActive && part.QuantityInStock >= detail.Quantity)
                .ExecuteUpdateAsync(update => update.SetProperty(
                    part => part.QuantityInStock,
                    part => part.QuantityInStock - detail.Quantity));

            if (updateCount == 0)
            {
                var part = await context.SpareParts.AsNoTracking()
                    .Where(item => item.Id == detail.SparePartId.Value)
                    .Select(item => new { item.IsActive })
                    .FirstOrDefaultAsync();
                await transaction.RollbackAsync();

                if (part is null)
                    return AddTicketDetailError.SparePartNotFound;
                if (!part.IsActive)
                    return AddTicketDetailError.SparePartInactive;
                return AddTicketDetailError.InsufficientStock;
            }
        }
        else
        {
            await transaction.RollbackAsync();
            return AddTicketDetailError.RepairServiceNotFoundOrInactive;
        }

        await context.RepairTicketDetails.AddAsync(detail);
        if (inventoryTransaction is not null)
            await context.InventoryTransactions.AddAsync(inventoryTransaction);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return AddTicketDetailError.None;
    }

    public async Task<EditTicketDetailError> UpdateAsync(int ticketId, int detailId, decimal quantity, decimal discountPercent)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ticket = await context.RepairTickets.FirstOrDefaultAsync(item => item.Id == ticketId);
        if (ticket is null) return await Rollback(transaction, EditTicketDetailError.RepairTicketNotFound);
        if (ticket.Status is not (RepairStatus.Diagnosing or RepairStatus.WaitingForApproval or RepairStatus.InProgress))
            return await Rollback(transaction, EditTicketDetailError.RepairTicketNotEditable);

        var detail = await context.RepairTicketDetails.FirstOrDefaultAsync(item =>
            item.Id == detailId && item.RepairTicketId == ticketId);
        if (detail is null) return await Rollback(transaction, EditTicketDetailError.DetailNotFound);
        if (quantity <= 0 || discountPercent is < 0 or > 100 || quantity != decimal.Round(quantity, 2) ||
            discountPercent != decimal.Round(discountPercent, 4))
            return await Rollback(transaction, EditTicketDetailError.InvalidAmounts);

        var quantityDelta = quantity - detail.Quantity;
        if (detail.SparePartId.HasValue && quantityDelta != 0)
        {
            var part = await context.SpareParts.FirstOrDefaultAsync(item => item.Id == detail.SparePartId.Value);
            if (part is null) return await Rollback(transaction, EditTicketDetailError.DetailNotFound);
            if (quantityDelta > 0 && !part.IsActive)
                return await Rollback(transaction, EditTicketDetailError.SparePartInactive);
            if (quantityDelta > 0 && part.QuantityInStock < quantityDelta)
                return await Rollback(transaction, EditTicketDetailError.InsufficientStock);

            part.QuantityInStock -= quantityDelta;
            await context.InventoryTransactions.AddAsync(new InventoryTransaction
            {
                SparePartId = part.Id,
                TransactionType = quantityDelta > 0 ? "StockOut" : "StockIn",
                Quantity = Math.Abs(quantityDelta),
                UnitCost = part.UnitCost,
                ReferenceNumber = ticket.TicketNumber,
                Note = $"Adjusted repair ticket detail #{detail.Id}"
            });
        }

        detail.Quantity = quantity;
        detail.DiscountPercent = discountPercent;
        detail.DiscountAmount = decimal.Round(detail.UnitPrice * quantity * discountPercent / 100m, 2);
        detail.TotalAmount = detail.UnitPrice * quantity - detail.DiscountAmount;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return EditTicketDetailError.None;
    }

    public async Task<EditTicketDetailError> DeleteAsync(int ticketId, int detailId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ticket = await context.RepairTickets.FirstOrDefaultAsync(item => item.Id == ticketId);
        if (ticket is null) return await Rollback(transaction, EditTicketDetailError.RepairTicketNotFound);
        if (ticket.Status is not (RepairStatus.Diagnosing or RepairStatus.WaitingForApproval or RepairStatus.InProgress))
            return await Rollback(transaction, EditTicketDetailError.RepairTicketNotEditable);

        var detail = await context.RepairTicketDetails.FirstOrDefaultAsync(item =>
            item.Id == detailId && item.RepairTicketId == ticketId);
        if (detail is null) return await Rollback(transaction, EditTicketDetailError.DetailNotFound);

        if (detail.SparePartId.HasValue)
        {
            var part = await context.SpareParts.FirstOrDefaultAsync(item => item.Id == detail.SparePartId.Value);
            if (part is null) return await Rollback(transaction, EditTicketDetailError.DetailNotFound);
            part.QuantityInStock += detail.Quantity;
            await context.InventoryTransactions.AddAsync(new InventoryTransaction
            {
                SparePartId = part.Id,
                TransactionType = "StockIn",
                Quantity = detail.Quantity,
                UnitCost = part.UnitCost,
                ReferenceNumber = ticket.TicketNumber,
                Note = $"Returned stock after removing repair ticket detail #{detail.Id}"
            });
        }

        context.RepairTicketDetails.Remove(detail);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return EditTicketDetailError.None;
    }

    private static async Task<T> Rollback<T>(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, T error)
    {
        await transaction.RollbackAsync();
        return error;
    }
}
