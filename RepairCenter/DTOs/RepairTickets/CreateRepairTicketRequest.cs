using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.RepairTickets;

public class CreateRepairTicketRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Range(1, int.MaxValue)]
    public int DeviceId { get; set; }

    [Required, StringLength(2000, MinimumLength = 1)]
    public string IssueDescription { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? CustomerNotes { get; set; }
}
