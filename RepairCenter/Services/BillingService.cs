using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services.Interfaces;
using RepairCenter.Contracts;

namespace RepairCenter.Services;

public class BillingService(IBillingRepository repository) : IBillingService
{
    public Task<PagedResult<BillingListRow>> GetBillingRowsAsync(int page, int pageSize, string? search) =>
        repository.GetBillingRowsAsync(page, pageSize, search);

    public Task<Invoice?> GetInvoiceAsync(int id) => repository.GetInvoiceAsync(id);
    public Task<Invoice?> GetInvoiceByTicketAsync(int ticketId) => repository.GetInvoiceByTicketAsync(ticketId);
    public Task<Warranty?> GetWarrantyByTicketAsync(int ticketId) => repository.GetWarrantyByTicketAsync(ticketId);
    public Task<Warranty?> GetWarrantyByCodeAsync(string code) => repository.GetWarrantyByCodeAsync(code);
    public Task<int> MarkOverdueInvoicesAsync(DateTime now) => repository.MarkOverdueInvoicesAsync(now);

    public async Task<(Invoice? Value, BillingError Error)> CreateInvoiceAsync(int ticketId, decimal discountPercent, decimal taxPercent, DateTime? dueAt, string? notes)
    {
        if (discountPercent is < 0 or > 100 || taxPercent is < 0 or > 100 ||
            discountPercent != decimal.Round(discountPercent, 4) || taxPercent != decimal.Round(taxPercent, 4))
            return (null, BillingError.InvalidAmount);
        var issuedAt = DateTime.UtcNow;
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant(),
            RepairTicketId = ticketId,
            IssuedAt = issuedAt,
            DueAt = dueAt.HasValue ? DateTime.SpecifyKind(dueAt.Value, DateTimeKind.Utc) : issuedAt.AddDays(30),
            DiscountPercent = discountPercent,
            TaxPercent = taxPercent,
            Notes = Normalize(notes)
        };
        var error = await repository.CreateInvoiceAsync(invoice);
        return error == BillingError.None ? (await repository.GetInvoiceAsync(invoice.Id), error) : (null, error);
    }

    public async Task<(InvoicePayment? Value, BillingError Error)> RecordPaymentAsync(
        int invoiceId, decimal amount, string method, string? reference, string? notes, int processorUserId)
    {
        method = method.Trim();
        if (amount <= 0 || amount != decimal.Round(amount, 2) || string.IsNullOrWhiteSpace(method))
            return (null, BillingError.InvalidAmount);
        var payment = new InvoicePayment
        {
            InvoiceId = invoiceId,
            Amount = amount,
            Type = InvoicePaymentType.Payment,
            ProcessedByUserId = processorUserId,
            PaymentMethod = method,
            ReferenceNumber = Normalize(reference),
            Notes = Normalize(notes),
            PaidAt = DateTime.UtcNow
        };
        var error = await repository.AddPaymentAsync(payment);
        return error == BillingError.None ? (payment, error) : (null, error);
    }

    public async Task<(InvoicePayment? Value, BillingError Error)> RefundPaymentAsync(
        int invoiceId, int paymentId, decimal amount, string method, string reason, int processorUserId)
    {
        if (amount <= 0 || amount != decimal.Round(amount, 2) || string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(reason))
            return (null, BillingError.InvalidAmount);
        var refund = NewRefund(invoiceId, paymentId, amount, method, reason, processorUserId);
        var error = await repository.RefundPaymentAsync(refund);
        return error == BillingError.None ? (refund, error) : (null, error);
    }

    public async Task<(InvoicePayment? Value, BillingError Error)> VoidPaymentAsync(
        int invoiceId, int paymentId, string method, string reason, int processorUserId)
    {
        if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(reason))
            return (null, BillingError.InvalidAmount);
        var refund = NewRefund(invoiceId, paymentId, 0, method, reason, processorUserId);
        var error = await repository.VoidPaymentAsync(refund);
        return error == BillingError.None ? (refund, error) : (null, error);
    }

    public async Task<(Warranty? Value, BillingError Error)> IssueWarrantyAsync(int ticketId)
    {
        var warranty = new Warranty
        {
            WarrantyCode = $"WAR-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            RepairTicketId = ticketId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Status = WarrantyStatus.Active
        };
        var error = await repository.CreateWarrantyAsync(warranty);
        return error == BillingError.None ? (await repository.GetWarrantyByTicketAsync(ticketId), error) : (null, error);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static InvoicePayment NewRefund(int invoiceId, int paymentId, decimal amount, string method, string reason, int processorUserId) => new()
    {
        InvoiceId = invoiceId,
        Amount = amount,
        Type = InvoicePaymentType.Refund,
        ProcessedByUserId = processorUserId,
        RefundOfPaymentId = paymentId,
        PaymentMethod = method.Trim(),
        Notes = reason.Trim(),
        PaidAt = DateTime.UtcNow
    };
}
