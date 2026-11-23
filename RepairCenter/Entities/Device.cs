namespace RepairCenter.Entities;

public class Device
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int DeviceTypeId { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }

    public Customer Customer { get; set; } = null!;
    public DeviceType DeviceType { get; set; } = null!;
    public ICollection<RepairTicket> RepairTickets { get; set; } = new List<RepairTicket>();
}
