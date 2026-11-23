using RepairCenter.Entities;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Services;

public class RepairTicketDetailService(IRepairTicketDetailRepository repository) : IRepairTicketDetailService
{
    public async Task<RepairTicketDetailResult<List<RepairTicketDetail>>> GetByTicketAsync(int ticketId)
    {
        if (!await repository.TicketExistsAsync(ticketId))
            return RepairTicketDetailResult<List<RepairTicketDetail>>.Failure(RepairTicketDetailError.RepairTicketNotFound);

        return RepairTicketDetailResult<List<RepairTicketDetail>>.Success(await repository.GetByTicketAsync(ticketId));
    }

    public async Task<RepairTicketDetailResult<RepairTicketDetail>> AddRepairServiceAsync(
        int ticketId, int serviceId, decimal quantity, decimal discountPercent,
        int? warrantyDuration = null, RepairCenter.Enums.WarrantyDurationUnit? warrantyDurationUnit = null, string? warrantyTerms = null)
    {
        var ticket = await repository.GetTicketAsync(ticketId);
        if (ticket is null)
            return Failure(RepairTicketDetailError.RepairTicketNotFound);
        if (!ValidAmounts(quantity, discountPercent))
            return Failure(RepairTicketDetailError.DiscountExceedsSubtotal);

        var service = await repository.GetActiveRepairServiceAsync(serviceId);
        if (service is null)
            return Failure(RepairTicketDetailError.RepairServiceNotFoundOrInactive);

        var unitPrice = ticket.IsWarrantyRepair ? 0 : service.UnitPrice;
        var subtotal = unitPrice * quantity;
        var discountAmount = decimal.Round(subtotal * discountPercent / 100m, 2);
        if (discountAmount > subtotal)
            return Failure(RepairTicketDetailError.DiscountExceedsSubtotal);

        var duration = ticket.IsWarrantyRepair ? 0 : warrantyDuration ?? service.WarrantyDuration;
        var durationUnit = warrantyDurationUnit ?? service.WarrantyDurationUnit;
        if (!ValidWarrantyPolicy(duration, durationUnit)) return Failure(RepairTicketDetailError.InvalidWarrantyPolicy);
        var detail = new RepairTicketDetail
        {
            RepairTicketId = ticketId,
            RepairServiceId = service.Id,
            Description = service.Name,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            TotalAmount = subtotal - discountAmount,
            WarrantyDuration = duration,
            WarrantyDurationUnit = durationUnit,
            WarrantyTerms = warrantyDuration.HasValue || warrantyDurationUnit.HasValue || warrantyTerms is not null
                ? Normalize(warrantyTerms) : service.WarrantyTerms
        };

        return await AddAsync(detail, null);
    }

    public async Task<RepairTicketDetailResult<RepairTicketDetail>> AddSparePartAsync(
        int ticketId, int sparePartId, decimal quantity, decimal discountPercent,
        int? warrantyDuration = null, RepairCenter.Enums.WarrantyDurationUnit? warrantyDurationUnit = null, string? warrantyTerms = null)
    {
        if (!await repository.TicketExistsAsync(ticketId))
            return Failure(RepairTicketDetailError.RepairTicketNotFound);
        if (!ValidAmounts(quantity, discountPercent))
            return Failure(RepairTicketDetailError.DiscountExceedsSubtotal);

        var ticket = await repository.GetTicketAsync(ticketId);
        if (ticket is null)
            return Failure(RepairTicketDetailError.RepairTicketNotFound);

        var part = await repository.GetSparePartAsync(sparePartId);
        if (part is null)
            return Failure(RepairTicketDetailError.SparePartNotFound);
        if (!part.IsActive)
            return Failure(RepairTicketDetailError.SparePartInactive);

        var subtotal = part.SellingPrice * quantity;
        var discountAmount = decimal.Round(subtotal * discountPercent / 100m, 2);
        if (discountAmount > subtotal)
            return Failure(RepairTicketDetailError.DiscountExceedsSubtotal);

        var duration = warrantyDuration ?? part.WarrantyDuration;
        var durationUnit = warrantyDurationUnit ?? part.WarrantyDurationUnit;
        if (!ValidWarrantyPolicy(duration, durationUnit)) return Failure(RepairTicketDetailError.InvalidWarrantyPolicy);
        var detail = new RepairTicketDetail
        {
            RepairTicketId = ticketId,
            SparePartId = part.Id,
            Description = part.Name,
            Quantity = quantity,
            UnitPrice = part.SellingPrice,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            TotalAmount = subtotal - discountAmount,
            WarrantyDuration = duration,
            WarrantyDurationUnit = durationUnit,
            WarrantyTerms = warrantyDuration.HasValue || warrantyDurationUnit.HasValue || warrantyTerms is not null
                ? Normalize(warrantyTerms) : part.WarrantyTerms
        };
        var stockOut = new InventoryTransaction
        {
            SparePartId = part.Id,
            TransactionType = "StockOut",
            Quantity = quantity,
            UnitCost = part.UnitCost,
            ReferenceNumber = ticket.TicketNumber,
            Note = $"Used for repair ticket {ticket.TicketNumber}"
        };

        return await AddAsync(detail, stockOut);
    }

    public async Task<RepairTicketDetailError> UpdateAsync(int ticketId, int detailId, decimal quantity, decimal discountPercent)
    {
        var error = await repository.UpdateAsync(ticketId, detailId, quantity, discountPercent);
        return error switch
        {
            EditTicketDetailError.None => RepairTicketDetailError.None,
            EditTicketDetailError.RepairTicketNotFound => RepairTicketDetailError.RepairTicketNotFound,
            EditTicketDetailError.RepairTicketNotEditable => RepairTicketDetailError.RepairTicketNotEditable,
            EditTicketDetailError.DetailNotFound => RepairTicketDetailError.DetailNotFound,
            EditTicketDetailError.InsufficientStock => RepairTicketDetailError.InsufficientStock,
            EditTicketDetailError.SparePartInactive => RepairTicketDetailError.SparePartInactive,
            _ => RepairTicketDetailError.InvalidAmounts
        };
    }

    public async Task<RepairTicketDetailError> DeleteAsync(int ticketId, int detailId)
    {
        var error = await repository.DeleteAsync(ticketId, detailId);
        return error switch
        {
            EditTicketDetailError.None => RepairTicketDetailError.None,
            EditTicketDetailError.RepairTicketNotFound => RepairTicketDetailError.RepairTicketNotFound,
            EditTicketDetailError.RepairTicketNotEditable => RepairTicketDetailError.RepairTicketNotEditable,
            EditTicketDetailError.DetailNotFound => RepairTicketDetailError.DetailNotFound,
            _ => RepairTicketDetailError.InvalidAmounts
        };
    }

    private async Task<RepairTicketDetailResult<RepairTicketDetail>> AddAsync(
        RepairTicketDetail detail, InventoryTransaction? inventoryTransaction)
    {
        var error = await repository.AddAsync(detail, inventoryTransaction);
        return error == AddTicketDetailError.None
            ? RepairTicketDetailResult<RepairTicketDetail>.Success(detail)
            : Failure(error switch
            {
                AddTicketDetailError.RepairTicketNotFound => RepairTicketDetailError.RepairTicketNotFound,
                AddTicketDetailError.RepairTicketClosed => RepairTicketDetailError.RepairTicketClosed,
                AddTicketDetailError.RepairServiceNotFoundOrInactive => RepairTicketDetailError.RepairServiceNotFoundOrInactive,
                AddTicketDetailError.SparePartNotFound => RepairTicketDetailError.SparePartNotFound,
                AddTicketDetailError.SparePartInactive => RepairTicketDetailError.SparePartInactive,
                AddTicketDetailError.InsufficientStock => RepairTicketDetailError.InsufficientStock,
                _ => RepairTicketDetailError.RepairTicketNotFound
            });
    }

    private static bool ValidAmounts(decimal quantity, decimal discountPercent) => quantity > 0 &&
        discountPercent is >= 0 and <= 100 && quantity == decimal.Round(quantity, 2) &&
        discountPercent == decimal.Round(discountPercent, 4);

    private static bool ValidWarrantyPolicy(int duration, RepairCenter.Enums.WarrantyDurationUnit unit) => unit switch
    {
        RepairCenter.Enums.WarrantyDurationUnit.Days => duration is >= 0 and <= 36500,
        RepairCenter.Enums.WarrantyDurationUnit.Months => duration is >= 0 and <= 120,
        _ => false
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RepairTicketDetailResult<RepairTicketDetail> Failure(RepairTicketDetailError error) =>
        RepairTicketDetailResult<RepairTicketDetail>.Failure(error);
}
