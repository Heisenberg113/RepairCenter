namespace RepairCenter.Entities;

public class RepairTicketDetail
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public int? RepairServiceId { get; set; }
    public int? SparePartId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int WarrantyDuration { get; set; }
    public RepairCenter.Enums.WarrantyDurationUnit WarrantyDurationUnit { get; set; } = RepairCenter.Enums.WarrantyDurationUnit.Months;
    public string? WarrantyTerms { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;
    public RepairService? RepairService { get; set; }
    public SparePart? SparePart { get; set; }
    public ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();
}
