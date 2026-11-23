using RepairCenter.Enums;

namespace RepairCenter.Entities;

public class Warranty
{
    public int Id { get; set; }
    public required string WarrantyCode { get; set; }
    public int RepairTicketId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public WarrantyStatus Status { get; set; } = WarrantyStatus.Active;
    public string? Terms { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;
    public User? VoidedByUser { get; set; }
    public ICollection<WarrantyClaim> Claims { get; set; } = new List<WarrantyClaim>();
}
