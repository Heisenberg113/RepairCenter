namespace RepairCenter.Contracts;

public sealed record RevenueReportResult(
    decimal TotalPayments,
    decimal TotalRefunds,
    IReadOnlyList<RevenueTransactionRow> Transactions,
    int TotalCount);
