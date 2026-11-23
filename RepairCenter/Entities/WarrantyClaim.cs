using RepairCenter.Enums;

namespace RepairCenter.Entities;

public class WarrantyClaim
{
    public int Id { get; set; }
    public int WarrantyId { get; set; }
    public int? RepairTicketDetailId { get; set; }
    public int RequestedByUserId { get; set; }
    public int? ResolvedByUserId { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public int? GeneratedRepairTicketId { get; set; }
    public required string Description { get; set; }
    public string? ResolutionNote { get; set; }
    public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public Warranty Warranty { get; set; } = null!;
    public RepairTicketDetail? RepairTicketDetail { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
    public Technician? AssignedTechnician { get; set; }
    public RepairTicket? GeneratedRepairTicket { get; set; }
}
