using System.ComponentModel.DataAnnotations;

namespace RepairCenter.DTOs.Device;

public class UpdateDeviceRequest
{
    [Range(1, int.MaxValue)]
    public int DeviceTypeId { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string Brand { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}
