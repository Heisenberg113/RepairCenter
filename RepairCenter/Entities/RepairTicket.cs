using RepairCenter.Enums;

namespace RepairCenter.Entities;

public class RepairTicket
{
    public int Id { get; set; }
    public required string TicketNumber { get; set; }
    public int CustomerId { get; set; }
    public int DeviceId { get; set; }
    public int? TechnicianId { get; set; }
    public int? ParentRepairTicketId { get; set; }
    public bool IsWarrantyRepair { get; set; }
    public RepairStatus Status { get; set; } = RepairStatus.Received;
    public required string IssueDescription { get; set; }
    public string? Diagnosis { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedCompletionAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }

    public Customer Customer { get; set; } = null!;
    public Device Device { get; set; } = null!;
    public Technician? Technician { get; set; }
    public ICollection<RepairTicketDetail> Details { get; set; } = new List<RepairTicketDetail>();
    public ICollection<RepairTicketStatusHistory> StatusHistory { get; set; } = new List<RepairTicketStatusHistory>();
    public Invoice? Invoice { get; set; }
    public Warranty? Warranty { get; set; }
    public RepairTicket? ParentRepairTicket { get; set; }
    public ICollection<RepairTicket> WarrantyRepairTickets { get; set; } = new List<RepairTicket>();
}
