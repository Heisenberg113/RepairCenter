using RepairCenter.Entities;
using RepairCenter.Contracts;

namespace RepairCenter.Repositories.Interfaces;

public interface IBillingRepository
{
    Task<PagedResult<BillingListRow>> GetBillingRowsAsync(int page, int pageSize, string? search);
    Task<Invoice?> GetInvoiceAsync(int id);
    Task<Invoice?> GetInvoiceByTicketAsync(int ticketId);
    Task<BillingError> CreateInvoiceAsync(Invoice invoice);
    Task<BillingError> AddPaymentAsync(InvoicePayment payment);
    Task<BillingError> RefundPaymentAsync(InvoicePayment refund);
    Task<BillingError> VoidPaymentAsync(InvoicePayment refund);
    Task<int> MarkOverdueInvoicesAsync(DateTime now);
    Task<Warranty?> GetWarrantyByTicketAsync(int ticketId);
    Task<Warranty?> GetWarrantyByCodeAsync(string code);
    Task<BillingError> CreateWarrantyAsync(Warranty warranty);
}
