namespace RepairCenter.DTOs.Device;

public class DeviceResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int DeviceTypeId { get; set; }
    public string? DeviceTypeName { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
}
