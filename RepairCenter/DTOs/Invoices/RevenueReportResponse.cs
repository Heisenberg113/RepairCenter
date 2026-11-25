namespace RepairCenter.DTOs.Invoices;

public class RevenueReportResponse
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetRevenue { get; set; }
    public List<RevenueTransactionResponse> Transactions { get; set; } = [];
    public int TotalCount { get; set; }
}
