using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Warranties;
public class ResolveWarrantyClaimRequest
{
    [Required, StringLength(1000, MinimumLength = 3)] public string Note { get; set; } = string.Empty;
}
