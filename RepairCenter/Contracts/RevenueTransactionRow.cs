using RepairCenter.Enums;

namespace RepairCenter.Contracts;

public sealed record RevenueTransactionRow(
    int Id,
    string InvoiceNumber,
    string TicketNumber,
    string CustomerName,
    InvoicePaymentType Type,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    DateTime PaidAt,
    string? ProcessedByUserName,
    string? Notes);
