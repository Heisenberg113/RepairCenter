using RepairCenter.Enums;

namespace RepairCenter.Contracts;

public sealed record BillingListRow(
    int TicketId,
    string TicketNumber,
    string CustomerName,
    string DeviceBrand,
    string DeviceModel,
    int? InvoiceId,
    string? InvoiceNumber,
    PaymentStatus? PaymentStatus);
