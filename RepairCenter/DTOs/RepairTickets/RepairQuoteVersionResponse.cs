namespace RepairCenter.DTOs.RepairTickets;

public class RepairQuoteVersionResponse
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = "[]";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Response { get; set; } = string.Empty;
    public DateTime? RespondedAt { get; set; }
    public string? ResponseChannel { get; set; }
    public string? ResponseNote { get; set; }
    public bool IsDuplicate { get; set; }
}
