using RepairCenter.Entities;
using RepairCenter.Contracts;

namespace RepairCenter.Services.Interfaces;

public interface IBillingService
{
    Task<PagedResult<BillingListRow>> GetBillingRowsAsync(int page, int pageSize, string? search);
    Task<Invoice?> GetInvoiceAsync(int id);
    Task<Invoice?> GetInvoiceByTicketAsync(int ticketId);
    Task<(Invoice? Value, BillingError Error)> CreateInvoiceAsync(int ticketId, decimal discountPercent, decimal taxPercent, DateTime? dueAt, string? notes);
    Task<(InvoicePayment? Value, BillingError Error)> RecordPaymentAsync(int invoiceId, decimal amount, string method, string? reference, string? notes, int processorUserId);
    Task<(InvoicePayment? Value, BillingError Error)> RefundPaymentAsync(int invoiceId, int paymentId, decimal amount, string method, string reason, int processorUserId);
    Task<(InvoicePayment? Value, BillingError Error)> VoidPaymentAsync(int invoiceId, int paymentId, string method, string reason, int processorUserId);
    Task<int> MarkOverdueInvoicesAsync(DateTime now);
    Task<Warranty?> GetWarrantyByTicketAsync(int ticketId);
    Task<Warranty?> GetWarrantyByCodeAsync(string code);
    Task<(Warranty? Value, BillingError Error)> IssueWarrantyAsync(int ticketId);
}
