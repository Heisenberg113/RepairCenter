using System.Data;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;

namespace RepairCenter.Repositories;

public class BillingRepository(RepairCenterDbContext context) : IBillingRepository
{
    public Task<Invoice?> GetInvoiceAsync(int id) => context.Invoices.AsNoTracking()
        .Include(x => x.RepairTicket).Include(x => x.Payments).ThenInclude(x => x.ProcessedByUser)
        .FirstOrDefaultAsync(x => x.Id == id);

    public Task<Invoice?> GetInvoiceByTicketAsync(int ticketId) => context.Invoices.AsNoTracking()
        .Include(x => x.RepairTicket).Include(x => x.Payments).ThenInclude(x => x.ProcessedByUser)
        .FirstOrDefaultAsync(x => x.RepairTicketId == ticketId);

    public async Task<BillingError> CreateInvoiceAsync(Invoice invoice)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ticket = await context.RepairTickets.Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == invoice.RepairTicketId);
        if (ticket is null) return await Rollback(tx, BillingError.TicketNotFound);
        if (ticket.Status is not (RepairStatus.Completed or RepairStatus.Delivered))
            return await Rollback(tx, BillingError.TicketNotCompleted);
        if (await context.Invoices.AnyAsync(x => x.RepairTicketId == ticket.Id))
            return await Rollback(tx, BillingError.InvoiceAlreadyExists);
        if (ticket.Details.Count == 0) return await Rollback(tx, BillingError.TicketHasNoDetails);

        invoice.Subtotal = ticket.Details.Sum(x => x.Quantity * x.UnitPrice);
        var lineDiscount = ticket.Details.Sum(x => x.DiscountAmount);
        var amountAfterLineDiscount = invoice.Subtotal - lineDiscount;
        var invoiceDiscount = decimal.Round(amountAfterLineDiscount * invoice.DiscountPercent / 100m, 2);
        var taxableAmount = amountAfterLineDiscount - invoiceDiscount;
        invoice.DiscountAmount = lineDiscount + invoiceDiscount;
        invoice.TaxAmount = decimal.Round(taxableAmount * invoice.TaxPercent / 100m, 2);
        invoice.TotalAmount = taxableAmount + invoice.TaxAmount;
        if (invoice.TotalAmount < 0) return await Rollback(tx, BillingError.InvalidAmount);
        if (invoice.TotalAmount == 0)
        {
            invoice.PaymentStatus = PaymentStatus.Paid;
            invoice.PaidAt = invoice.IssuedAt;
        }
        else if (invoice.DueAt <= DateTime.UtcNow)
        {
            invoice.PaymentStatus = PaymentStatus.Overdue;
        }

        await context.Invoices.AddAsync(invoice);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return BillingError.None;
    }

    public async Task<BillingError> AddPaymentAsync(InvoicePayment payment)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var invoice = await context.Invoices.FirstOrDefaultAsync(x => x.Id == payment.InvoiceId);
        if (invoice is null) return await Rollback(tx, BillingError.InvoiceNotFound);
        if (payment.Amount <= 0) return await Rollback(tx, BillingError.InvalidAmount);
        if (payment.Amount != decimal.Round(payment.Amount, 2))
            return await Rollback(tx, BillingError.InvalidAmount);
        if (!await IsActiveUserAsync(payment.ProcessedByUserId))
            return await Rollback(tx, BillingError.InvalidProcessor);
        if (invoice.AmountPaid >= invoice.TotalAmount)
            return await Rollback(tx, BillingError.InvoiceNotIssued);
        if (invoice.AmountPaid + payment.Amount > invoice.TotalAmount)
            return await Rollback(tx, BillingError.Overpayment);

        payment.Type = InvoicePaymentType.Payment;
        payment.RefundOfPaymentId = null;
        invoice.AmountPaid += payment.Amount;
        invoice.PaymentStatus = GetPaymentStatus(invoice);
        invoice.PaidAt = invoice.PaymentStatus == PaymentStatus.Paid ? payment.PaidAt : null;
        await context.InvoicePayments.AddAsync(payment);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return BillingError.None;
    }

    public Task<BillingError> RefundPaymentAsync(InvoicePayment refund) => ProcessRefundAsync(refund, voidRemaining: false);

    public Task<BillingError> VoidPaymentAsync(InvoicePayment refund) => ProcessRefundAsync(refund, voidRemaining: true);

    private async Task<BillingError> ProcessRefundAsync(InvoicePayment refund, bool voidRemaining)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await IsActiveUserAsync(refund.ProcessedByUserId))
            return await Rollback(tx, BillingError.InvalidProcessor);
        var payment = await context.InvoicePayments
            .Include(x => x.Refunds)
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == refund.RefundOfPaymentId && x.InvoiceId == refund.InvoiceId && x.Type == InvoicePaymentType.Payment);
        if (payment is null) return await Rollback(tx, BillingError.PaymentNotFound);

        var available = payment.Amount - payment.Refunds.Sum(x => x.Amount);
        if (available <= 0) return await Rollback(tx, BillingError.PaymentNotRefundable);
        if (voidRemaining) refund.Amount = available;
        if (refund.Amount <= 0 || refund.Amount != decimal.Round(refund.Amount, 2))
            return await Rollback(tx, BillingError.InvalidAmount);
        if (refund.Amount > available || refund.Amount > payment.Invoice.AmountPaid)
            return await Rollback(tx, BillingError.RefundExceedsAvailable);

        refund.Type = InvoicePaymentType.Refund;
        refund.PaidAt = DateTime.UtcNow;
        payment.Invoice.AmountPaid -= refund.Amount;
        payment.Invoice.PaymentStatus = payment.Invoice.AmountPaid == 0
            ? PaymentStatus.Refunded
            : GetPaymentStatus(payment.Invoice);
        payment.Invoice.PaidAt = null;
        await context.InvoicePayments.AddAsync(refund);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return BillingError.None;
    }

    public async Task<int> MarkOverdueInvoicesAsync(DateTime now)
    {
        return await context.Invoices
            .Where(invoice => invoice.DueAt.HasValue && invoice.DueAt <= now && invoice.TotalAmount > invoice.AmountPaid &&
                (invoice.PaymentStatus == PaymentStatus.Pending || invoice.PaymentStatus == PaymentStatus.PartiallyPaid))
            .ExecuteUpdateAsync(update => update.SetProperty(invoice => invoice.PaymentStatus, PaymentStatus.Overdue));
    }

    private Task<bool> IsActiveUserAsync(int? userId) => userId.HasValue
        ? context.Users.AnyAsync(user => user.Id == userId.Value && user.IsActive)
        : Task.FromResult(false);

    private static PaymentStatus GetPaymentStatus(Invoice invoice)
    {
        if (invoice.AmountPaid >= invoice.TotalAmount) return PaymentStatus.Paid;
        if (invoice.DueAt.HasValue && invoice.DueAt.Value <= DateTime.UtcNow)
            return PaymentStatus.Overdue;
        return invoice.AmountPaid > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Pending;
    }

    public Task<Warranty?> GetWarrantyByTicketAsync(int ticketId) => context.Warranties.AsNoTracking()
        .Include(x => x.RepairTicket).ThenInclude(x => x.Details)
        .Include(x => x.RepairTicket).ThenInclude(x => x.Customer)
        .FirstOrDefaultAsync(x => x.RepairTicketId == ticketId);

    public Task<Warranty?> GetWarrantyByCodeAsync(string code) => context.Warranties.AsNoTracking()
        .Include(x => x.RepairTicket).ThenInclude(x => x.Details)
        .Include(x => x.RepairTicket).ThenInclude(x => x.Customer)
        .FirstOrDefaultAsync(x => x.WarrantyCode == code);

    public async Task<BillingError> CreateWarrantyAsync(Warranty warranty)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ticket = await context.RepairTickets.Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == warranty.RepairTicketId);
        if (ticket is null) return await Rollback(tx, BillingError.TicketNotFound);
        if (ticket.Status != RepairStatus.Delivered) return await Rollback(tx, BillingError.TicketNotDelivered);
        if (await context.Warranties.AnyAsync(x => x.RepairTicketId == ticket.Id))
            return await Rollback(tx, BillingError.WarrantyAlreadyExists);
        var today = ticket.DeliveredAt?.Date ?? DateTime.UtcNow.Date;
        var coveredDetails = ticket.Details.Where(x => x.WarrantyDuration > 0).ToList();
        if (coveredDetails.Count == 0) return await Rollback(tx, BillingError.NoWarrantyCoverage);
        if (coveredDetails.Any(detail => detail.WarrantyDurationUnit switch
            {
                WarrantyDurationUnit.Days => detail.WarrantyDuration > 36500,
                WarrantyDurationUnit.Months => detail.WarrantyDuration > 120,
                _ => true
            })) return await Rollback(tx, BillingError.InvalidWarrantyPolicy);
        foreach (var detail in coveredDetails)
        {
            detail.WarrantyStartDate = today;
            detail.WarrantyEndDate = detail.WarrantyDurationUnit == WarrantyDurationUnit.Months
                ? today.AddMonths(detail.WarrantyDuration)
                : today.AddDays(detail.WarrantyDuration);
        }
        warranty.StartDate = today;
        warranty.EndDate = coveredDetails.Max(x => x.WarrantyEndDate!.Value);
        await context.Warranties.AddAsync(warranty);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return BillingError.None;
    }

    private static async Task<BillingError> Rollback(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, BillingError error)
    {
        await tx.RollbackAsync();
        return error;
    }
}
