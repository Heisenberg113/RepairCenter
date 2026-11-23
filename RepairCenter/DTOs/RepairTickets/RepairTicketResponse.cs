using RepairCenter.Enums;

namespace RepairCenter.DTOs.RepairTickets;

public class RepairTicketResponse
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int DeviceId { get; set; }
    public int DeviceTypeId { get; set; }
    public int? TechnicianId { get; set; }
    public int? ParentRepairTicketId { get; set; }
    public string? ParentRepairTicketNumber { get; set; }
    public bool IsWarrantyRepair { get; set; }
    public string? TechnicianName { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceBrand { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public RepairStatus Status { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public string? CustomerNotes { get; set; }
    public DateTime ReceivedAt { get; set; }
}
