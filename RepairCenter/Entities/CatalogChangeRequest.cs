namespace RepairCenter.Entities;

public class CatalogChangeRequest
{
    public int Id { get; set; }
    public required string ItemType { get; set; }
    public required string Action { get; set; }
    public int? TargetId { get; set; }
    public required string PayloadJson { get; set; }
    public string Status { get; set; } = "Pending";
    public int RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public int? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
}
