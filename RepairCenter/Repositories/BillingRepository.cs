using System.Data;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Contracts;
using RepairCenter.Utilities;

namespace RepairCenter.Repositories;

public class BillingRepository(RepairCenterDbContext context) : IBillingRepository
{
    public async Task<RevenueReportResult> GetRevenueReportAsync(DateOnly from, DateOnly to)
    {
        var fromUtc = Gmt7Clock.ToUtcStartOfDay(from);
        var toUtc = Gmt7Clock.ToUtcEndOfDay(to);
        var matchingPayments = context.InvoicePayments.AsNoTracking()
            .Where(payment => payment.PaidAt >= fromUtc && payment.PaidAt <= toUtc);
        var aggregates = await matchingPayments
            .GroupBy(payment => payment.Type)
            .Select(group => new
            {
                Type = group.Key,
                Amount = group.Sum(payment => payment.Amount),
                Count = group.Count()
            })
            .ToListAsync();
        var totalPayments = aggregates
            .Where(group => group.Type == InvoicePaymentType.Payment)
            .Select(group => group.Amount)
            .FirstOrDefault();
        var totalRefunds = aggregates
            .Where(group => group.Type == InvoicePaymentType.Refund)
            .Select(group => group.Amount)
            .FirstOrDefault();
        var totalCount = aggregates.Sum(group => group.Count);
        var transactions = await matchingPayments
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .Select(payment => new RevenueTransactionRow(
                payment.Id,
                payment.Invoice.InvoiceNumber,
                payment.Invoice.RepairTicket.TicketNumber,
                payment.Invoice.RepairTicket.Customer.Name,
                payment.Type,
                payment.Amount,
                payment.PaymentMethod,
                payment.ReferenceNumber,
                payment.PaidAt,
                payment.ProcessedByUser == null ? null : payment.ProcessedByUser.FullName,
                payment.Notes))
            .ToListAsync();

        return new RevenueReportResult(totalPayments, totalRefunds, transactions, totalCount);
    }

    public async Task<PagedResult<BillingListRow>> GetBillingRowsAsync(
        int page, int pageSize, string? search, BillingListStatus? status, bool dueDateDescending)
    {
        var tickets = context.RepairTickets.AsNoTracking()
            .Where(x => x.Status == RepairStatus.Completed || x.Status == RepairStatus.Delivered);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            tickets = tickets.Where(x =>
                x.TicketNumber.Contains(term) ||
                x.Customer.Name.Contains(term) ||
                x.Device.Brand.Contains(term) ||
                x.Device.Model.Contains(term));
        }

        var joined = from ticket in tickets
            join invoice in context.Invoices.AsNoTracking()
                on ticket.Id equals invoice.RepairTicketId into ticketInvoices
            from invoice in ticketInvoices.DefaultIfEmpty()
            select new { Ticket = ticket, Invoice = invoice };

        joined = status switch
        {
            BillingListStatus.NoInvoice => joined.Where(x => x.Invoice == null),
            BillingListStatus.Pending => joined.Where(x => x.Invoice != null && x.Invoice.PaymentStatus == PaymentStatus.Pending),
            BillingListStatus.PartiallyPaid => joined.Where(x => x.Invoice != null && x.Invoice.PaymentStatus == PaymentStatus.PartiallyPaid),
            BillingListStatus.Paid => joined.Where(x => x.Invoice != null && x.Invoice.PaymentStatus == PaymentStatus.Paid),
            BillingListStatus.Refunded => joined.Where(x => x.Invoice != null && x.Invoice.PaymentStatus == PaymentStatus.Refunded),
            BillingListStatus.Overdue => joined.Where(x => x.Invoice != null && x.Invoice.PaymentStatus == PaymentStatus.Overdue),
            _ => joined
        };

        var totalCount = await joined.CountAsync();
        var ordered = dueDateDescending
            ? joined.OrderBy(x => x.Invoice == null || !x.Invoice.DueAt.HasValue)
                .ThenByDescending(x => x.Invoice!.DueAt)
                .ThenByDescending(x => x.Ticket.CompletedAt ?? x.Ticket.DeliveredAt ?? x.Ticket.ReceivedAt)
                .ThenByDescending(x => x.Ticket.Id)
            : joined.OrderBy(x => x.Invoice == null || !x.Invoice.DueAt.HasValue)
                .ThenBy(x => x.Invoice!.DueAt)
                .ThenByDescending(x => x.Ticket.CompletedAt ?? x.Ticket.DeliveredAt ?? x.Ticket.ReceivedAt)
                .ThenByDescending(x => x.Ticket.Id);
        var rows = await ordered
            .Select(x => new BillingListRow(
                x.Ticket.Id,
                x.Ticket.TicketNumber,
                x.Ticket.Customer.Name,
                x.Ticket.Device.Brand,
                x.Ticket.Device.Model,
                x.Invoice == null ? null : x.Invoice.Id,
                x.Invoice == null ? null : x.Invoice.InvoiceNumber,
                x.Invoice == null ? null : x.Invoice.PaymentStatus,
                x.Invoice == null ? null : x.Invoice.TotalAmount - x.Invoice.AmountPaid,
                x.Invoice == null ? null : x.Invoice.DueAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BillingListRow>(rows, page, pageSize, totalCount);
    }

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
        else if (invoice.DueAt.HasValue &&
                 DateOnly.FromDateTime(invoice.DueAt.Value) < Gmt7Clock.Today)
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
        var startOfToday = Gmt7Clock.ToUtcMidnight(Gmt7Clock.FromUtc(now));
        return await context.Invoices
            .Where(invoice => invoice.DueAt.HasValue && invoice.DueAt < startOfToday &&
                invoice.TotalAmount > invoice.AmountPaid &&
                (invoice.PaymentStatus == PaymentStatus.Pending || invoice.PaymentStatus == PaymentStatus.PartiallyPaid))
            .ExecuteUpdateAsync(update => update.SetProperty(invoice => invoice.PaymentStatus, PaymentStatus.Overdue));
    }

    private Task<bool> IsActiveUserAsync(int? userId) => userId.HasValue
        ? context.Users.AnyAsync(user => user.Id == userId.Value && user.IsActive)
        : Task.FromResult(false);

    private static PaymentStatus GetPaymentStatus(Invoice invoice)
    {
        if (invoice.AmountPaid >= invoice.TotalAmount) return PaymentStatus.Paid;
        if (invoice.DueAt.HasValue &&
            DateOnly.FromDateTime(invoice.DueAt.Value) < Gmt7Clock.Today)
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
        var today = ticket.DeliveredAt.HasValue
            ? Gmt7Clock.FromUtc(ticket.DeliveredAt.Value)
            : Gmt7Clock.Today;
        var todayUtc = Gmt7Clock.ToUtcMidnight(today);
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
            detail.WarrantyStartDate = todayUtc;
            detail.WarrantyEndDate = detail.WarrantyDurationUnit == WarrantyDurationUnit.Months
                ? todayUtc.AddMonths(detail.WarrantyDuration)
                : todayUtc.AddDays(detail.WarrantyDuration);
        }
        warranty.StartDate = todayUtc;
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
