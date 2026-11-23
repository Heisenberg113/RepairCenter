namespace RepairCenter.Entities;

public class DeviceType
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<TechnicianDeviceType> Technicians { get; set; } = new List<TechnicianDeviceType>();
    public ICollection<RepairService> RepairServices { get; set; } = new List<RepairService>();
    public ICollection<SparePart> SpareParts { get; set; } = new List<SparePart>();
}
