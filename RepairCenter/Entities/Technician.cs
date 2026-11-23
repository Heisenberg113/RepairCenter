namespace RepairCenter.Entities;

public class Technician
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public required string EmployeeCode { get; set; }
    public required string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Specialty { get; set; }
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
    public ICollection<RepairTicket> RepairTickets { get; set; } = new List<RepairTicket>();
    public ICollection<TechnicianDeviceType> DeviceTypes { get; set; } = new List<TechnicianDeviceType>();
}
