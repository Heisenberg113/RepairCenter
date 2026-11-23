using RepairCenter.Entities;

namespace RepairCenter.Repositories.Interfaces;

public enum BillingError { None, TicketNotFound, TicketNotCompleted, TicketHasNoDetails, InvoiceNotFound, InvoiceAlreadyExists, InvoiceNotIssued, InvalidAmount, Overpayment, WarrantyNotFound, TicketNotDelivered, WarrantyAlreadyExists, InvalidProcessor, PaymentNotFound, PaymentNotRefundable, RefundExceedsAvailable, NoWarrantyCoverage, InvalidWarrantyPolicy }

public interface IBillingRepository
{
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
