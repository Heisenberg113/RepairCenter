using RepairCenter.Enums;

namespace RepairCenter.DTOs.Warranties;

public sealed class WarrantyItemResponse
{
    public int RepairTicketDetailId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int WarrantyDuration { get; set; }
    public WarrantyDurationUnit WarrantyDurationUnit { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsInWarranty { get; set; }
    public string? Terms { get; set; }
}
