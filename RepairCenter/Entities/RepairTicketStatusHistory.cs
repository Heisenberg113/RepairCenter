using RepairCenter.Enums;

namespace RepairCenter.Entities;

public class RepairTicketStatusHistory
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public RepairStatus OldStatus { get; set; }
    public RepairStatus NewStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
