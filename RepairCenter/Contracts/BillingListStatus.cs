namespace RepairCenter.Contracts;

public enum BillingListStatus
{
    NoInvoice,
    Pending,
    PartiallyPaid,
    Paid,
    Refunded,
    Overdue
}
