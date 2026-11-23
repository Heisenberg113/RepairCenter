namespace RepairCenter.Entities;

public class RepairQuoteVersion
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = "[]";
    public string SnapshotHash { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Response { get; set; } = "Pending";
    public int? RespondedByUserId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseChannel { get; set; }
    public string? ResponseNote { get; set; }
    public RepairTicket RepairTicket { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public User? RespondedByUser { get; set; }
}
