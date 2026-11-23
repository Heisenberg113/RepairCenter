namespace RepairCenter.Entities;

public class TechnicianDeviceType
{
    public int TechnicianId { get; set; }
    public int DeviceTypeId { get; set; }

    public Technician Technician { get; set; } = null!;
    public DeviceType DeviceType { get; set; } = null!;
}
