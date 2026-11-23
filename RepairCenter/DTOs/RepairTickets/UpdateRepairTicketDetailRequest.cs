using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.RepairTickets;

public sealed class UpdateRepairTicketDetailRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal DiscountPercent { get; set; }
}
