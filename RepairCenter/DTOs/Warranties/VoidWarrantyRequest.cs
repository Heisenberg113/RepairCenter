using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Warranties;
public class VoidWarrantyRequest
{
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = string.Empty;
}
