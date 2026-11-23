namespace RepairCenter.Entities;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<RepairTicket> RepairTickets { get; set; } = new List<RepairTicket>();
}
