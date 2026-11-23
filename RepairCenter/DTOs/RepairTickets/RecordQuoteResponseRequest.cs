using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.RepairTickets;

public class RecordQuoteResponseRequest
{
    [Required, RegularExpression("^(Accepted|Rejected|RevisionRequested)$")]
    public string Response { get; set; } = string.Empty;
    [Required, StringLength(40, MinimumLength = 2)] public string Channel { get; set; } = string.Empty;
    [StringLength(1000)] public string? Note { get; set; }
}
