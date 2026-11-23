using RepairCenter.Enums;

namespace RepairCenter.DTOs.RepairTickets;

public class RepairTicketStatusHistoryResponse
{
    public int Id { get; set; }
    public RepairStatus OldStatus { get; set; }
    public RepairStatus NewStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
}
