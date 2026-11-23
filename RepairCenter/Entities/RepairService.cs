namespace RepairCenter.Entities;

public class RepairService
{
    public int Id { get; set; }
    public int? DeviceTypeId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int WarrantyDuration { get; set; }
    public RepairCenter.Enums.WarrantyDurationUnit WarrantyDurationUnit { get; set; } = RepairCenter.Enums.WarrantyDurationUnit.Months;
    public string? WarrantyTerms { get; set; }

    public DeviceType? DeviceType { get; set; }
    public ICollection<RepairTicketDetail> RepairTicketDetails { get; set; } = new List<RepairTicketDetail>();
}
