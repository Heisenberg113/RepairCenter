namespace RepairCenter.DTOs.Technicians;

public class TechnicianResponse
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Specialty { get; set; }
    public bool IsActive { get; set; }
    public List<DeviceTypeOptionResponse> DeviceTypes { get; set; } = [];
}
