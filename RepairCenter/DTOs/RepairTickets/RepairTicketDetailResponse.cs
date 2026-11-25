using RepairCenter.Enums;

namespace RepairCenter.DTOs.RepairTickets;

public class RepairTicketDetailResponse
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public int? RepairServiceId { get; set; }
    public int? SparePartId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int WarrantyDuration { get; set; }
    public WarrantyDurationUnit WarrantyDurationUnit { get; set; }
    public string? WarrantyTerms { get; set; }
    public DateOnly? WarrantyStartDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
}
