using RepairCenter.Enums;
using RepairCenter.Utilities;

namespace RepairCenter.DTOs.Warranties;

public class WarrantyResponse
{
    public int Id { get; set; }
    public string WarrantyCode { get; set; } = string.Empty;
    public int RepairTicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhoneNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public WarrantyStatus Status { get; set; }
    public bool IsInWarranty
    {
        get
        {
            var today = Gmt7Clock.Today;
            return Status == WarrantyStatus.Active && StartDate <= today && today <= EndDate;
        }
    }
    public string? Terms { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedByUserId { get; set; }
    public List<WarrantyItemResponse> Items { get; set; } = [];
}
