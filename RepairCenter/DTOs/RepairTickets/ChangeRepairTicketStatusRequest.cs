using System.ComponentModel.DataAnnotations;
using RepairCenter.Enums;

namespace RepairCenter.DTOs.RepairTickets;

public class ChangeRepairTicketStatusRequest
{
    [Required, EnumDataType(typeof(RepairStatus))]
    public RepairStatus? NewStatus { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
