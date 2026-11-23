using System.ComponentModel.DataAnnotations;
namespace RepairCenter.DTOs.Warranties;
public class AssignWarrantyClaimRequest
{
    [Range(1, int.MaxValue)] public int? TechnicianId { get; set; }
}
