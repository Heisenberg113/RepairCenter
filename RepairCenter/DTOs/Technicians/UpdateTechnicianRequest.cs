using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.Technicians;

public class UpdateTechnicianRequest
{
    public int? UserId { get; set; }

    [Required, StringLength(30, MinimumLength = 1)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Specialty { get; set; }

    public bool IsActive { get; set; } = true;
    public List<int> DeviceTypeIds { get; set; } = [];
}
