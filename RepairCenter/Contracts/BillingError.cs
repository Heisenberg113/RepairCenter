namespace RepairCenter.Contracts;

public enum BillingError
{
    None,
    TicketNotFound,
    TicketNotCompleted,
    TicketHasNoDetails,
    InvoiceNotFound,
    InvoiceAlreadyExists,
    InvoiceNotIssued,
    InvalidAmount,
    Overpayment,
    WarrantyNotFound,
    TicketNotDelivered,
    WarrantyAlreadyExists,
    InvalidProcessor,
    PaymentNotFound,
    PaymentNotRefundable,
    RefundExceedsAvailable,
    NoWarrantyCoverage,
    InvalidWarrantyPolicy
}
