using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Warranties;
public class CreateWarrantyClaimRequest
{
    [Range(1, int.MaxValue)] public int RepairTicketDetailId { get; set; }
    [Required, StringLength(2000, MinimumLength = 5)] public string Description { get; set; } = string.Empty;
}
