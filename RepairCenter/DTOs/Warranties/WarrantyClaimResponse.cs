using RepairCenter.Enums;
namespace RepairCenter.DTOs.Warranties;
public class WarrantyClaimResponse
{
    public int Id { get; set; }
    public int WarrantyId { get; set; }
    public string WarrantyCode { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int? RepairTicketDetailId { get; set; }
    public string? ItemDescription { get; set; }
    public int? GeneratedRepairTicketId { get; set; }
    public string? GeneratedRepairTicketNumber { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public int? ResolvedByUserId { get; set; }
    public string? ResolvedByUserName { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public int? AssignedTechnicianUserId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public int DeviceTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
    public WarrantyClaimStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
